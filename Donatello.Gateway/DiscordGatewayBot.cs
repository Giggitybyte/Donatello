namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello;
using Donatello.Entity;
using Donatello.Enumeration;
using Donatello.Extension.Internal;
using Donatello.Gateway.Event;
using Donatello.Gateway.Extension;
using Microsoft.Extensions.Logging;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through one or more websocket connections.<br/> 
/// Sends requests to the API through HTTP REST requests and a websocket connection.
/// </remarks>
public sealed class DiscordGatewayBot : DiscordBot
{
    private DiscordWebsocketShard[] _shards;
    private GatewayIntent _intents;
    private Subject<DiscordEvent> _eventSequence;
    private List<DiscordSnowflake> _unavailableGuilds;
    private DiscordSnowflake _id;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="logger"></param>
    public DiscordGatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILogger logger = null) : base(token, logger)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _shards = Array.Empty<DiscordWebsocketShard>();
        _eventSequence = new Subject<DiscordEvent>();
    }

    /// <summary></summary>
    internal IReadOnlyList<DiscordWebsocketShard> Shards => new ReadOnlyCollection<DiscordWebsocketShard>(_shards);

    /// <summary></summary>
    public IObservable<DiscordEvent> Events => _eventSequence;

    /// <summary>Connects to the Discord gateway.</summary>
    public override async ValueTask StartAsync()
    {
        var websocketMetadata = await this.RestClient.GetGatewayMetadataAsync();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var clusterSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new DiscordWebsocketShard[shardCount];

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new DiscordWebsocketShard(shardId, this.Logger);

            shard.Events.Where(e => e.GetProperty("op").GetInt32() is 0)
                .Subscribe(e => this.PublishDiscordEventAsync(shard, e).ToObservable());

            shard.Events.Where(e => e.GetProperty("op").GetInt32() is 10)
                .Subscribe(e => this.SendShardIdentityAsync(shard).ToObservable());

            _shards[shardId] = shard;
        }

        await Observable.Generate(0, (i => i < shardCount), (i => i + clusterSize), (i => _shards.Skip(i).Take(clusterSize)), (i => TimeSpan.FromSeconds(5)))
            .SelectMany(cluster => cluster.ToObservable())
            .SelectMany(shard => shard.ConnectAsync().ToObservable());
    }

    /// <summary>Closes all websocket connections.</summary>
    public override async ValueTask StopAsync()
    {
        if (_shards.Length is 0)
            throw new InvalidOperationException("This instance is not currently connected to Discord.");

        var disconnectTasks = new Task[_shards.Length];

        foreach (var shard in _shards)
            disconnectTasks[shard.Id] = shard.DisconnectAsync();

        await Task.WhenAll(disconnectTasks);
        Array.Clear(_shards, 0, _shards.Length);
    }

    public ValueTask<DiscordUser> GetSelfAsync()
        => this.GetUserAsync(_id);

    /// <summary></summary>
    private async Task SendShardIdentityAsync(DiscordWebsocketShard shard)
    {
        if (shard.SessionId is not null)
        {
            await shard.SendPayloadAsync(6, jsonWriter =>
            {
                jsonWriter.WriteString("token", this.Token);
                jsonWriter.WriteString("session_id", shard.SessionId);
                jsonWriter.WriteNumber("seq", shard.EventIndex);
            });
        }
        else
        {
            await shard.SendPayloadAsync(2, jsonWriter =>
            {
                jsonWriter.WriteString("token", this.Token);

                jsonWriter.WriteStartObject("properties");
                jsonWriter.WriteString("os", Environment.OSVersion.ToString());
                jsonWriter.WriteString("browser", "Donatello/0.0.0");
                jsonWriter.WriteString("device", "Donatello/0.0.0");
                jsonWriter.WriteEndObject();

                jsonWriter.WriteStartArray("shard");
                jsonWriter.WriteNumberValue(shard.Id);
                jsonWriter.WriteNumberValue(_shards.Length);
                jsonWriter.WriteEndArray();

                jsonWriter.WriteNumber("intents", (int)_intents);
                jsonWriter.WriteNumber("large_threshold", 250);
                // json.WriteBoolean("compress", true);
            });
        }
    }

    /// <summary></summary>
    private async Task PublishDiscordEventAsync(DiscordWebsocketShard shard, JsonElement gatewayEvent)
    {
        var eventName = gatewayEvent.GetProperty("t").GetString();
        var eventJson = gatewayEvent.GetProperty("d");

        DiscordEvent eventObject = eventName switch
        {
            "READY" => Connected(),
            "CHANNEL_CREATE" => ChannelCreated(),
            "CHANNEL_UPDATE" => ChannelUpdated(),
            "CHANNEL_DELETE" => ChannelDeleted(),
            "THREAD_CREATE" => await ThreadCreated(),
            "THREAD_UPDATE" => await ThreadUpdated(),
            "THREAD_DELETE" => await ThreadDeleted(),
            "THREAD_LIST_SYNC" => 

            _ => Unknown()
        };

        eventObject.Bot = this;
        eventObject.Shard = shard;

        _eventSequence.OnNext(eventObject);


        ConnectedEvent Connected() // maybe these should be observable sequences??? yes
        {
            var shardId = eventJson.GetProperty("shard").EnumerateArray().First().GetInt16();

            foreach (var partialGuild in eventJson.GetProperty("guilds").EnumerateArray())
                _unavailableGuilds.Add(partialGuild.GetProperty("id").ToSnowflake());

            var user = new DiscordUser(this, eventJson.GetProperty("user"));
            _id = user.Id;
            this.UserCache.Add(user.Id, user);

            return new()
            {
                Bot = this,
                Shard = _shards[shardId],
                User = user
            };
        }

        EntityCreatedEvent<DiscordChannel> ChannelCreated()
        {
            var channel = eventJson.ToChannelEntity(this);
            this.ChannelCache.Add(channel.Id, channel);

            return new() 
            { 
                Entity = channel 
            };
        }

        EntityUpdatedEvent<DiscordChannel> ChannelUpdated()
        {
            var updatedChannel = eventJson.ToChannelEntity(this);
            var outdatedChannel = this.ChannelCache.Replace(updatedChannel.Id, updatedChannel);

            return new()
            {
                UpdatedEntity = updatedChannel,
                OutdatedEnity = outdatedChannel
            };
        }

        EntityDeletedEvent<DiscordChannel> ChannelDeleted()
        {
            var channelId = eventJson.GetProperty("id").ToSnowflake();
            var cachedChannel = this.ChannelCache.Remove(channelId);

            return new()
            {
                EntityId = channelId,
                CachedEntity = cachedChannel
            };
        }

        async ValueTask<EntityCreatedEvent<DiscordThreadTextChannel>> ThreadCreated()
        {
            var threadChannel = eventJson.ToChannelEntity(this) as DiscordThreadTextChannel;
            var guild = await threadChannel.GetGuildAsync();
            guild.ThreadCache.Add(threadChannel.Id, threadChannel);

            return new() 
            { 
                Entity = threadChannel 
            };
        }

        async ValueTask<EntityUpdatedEvent<DiscordThreadTextChannel>> ThreadUpdated()
        {
            var updatedThread = eventJson.ToChannelEntity(this) as DiscordThreadTextChannel;
            var guild = await updatedThread.GetGuildAsync();
            var outdatedThread = guild.ThreadCache.Replace(updatedThread.Id, updatedThread);

            return new()
            {
                UpdatedEntity = updatedThread,
                OutdatedEnity = outdatedThread
            };
        }

        async ValueTask<EntityDeletedEvent<DiscordThreadTextChannel>> ThreadDeleted()
        {
            var threadId = eventJson.GetProperty("id").ToSnowflake();
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
            var guild = await this.GetGuildAsync(guildId);
            var cachedThread = guild.ThreadCache.Remove(threadId);

            return new()
            {
                EntityId = threadId,
                CachedEntity = cachedThread
            };
        }

        

        UnknownEvent Unknown()
        {
            this.Logger.LogWarning("Received unknown gateway event: {EventName}", eventName);
            return new() 
            { 
                Name = eventName, 
                Json = eventJson 
            };
        }
    }
}
