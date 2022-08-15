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
    private DiscordSnowflake _botUserId;

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

    /// <summary></summary>
    public DiscordUser Self { get; private set; }

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
                .SelectMany(e => PublishDiscordEventAsync(e).ToObservable())
                .Subscribe();

            shard.Events.Where(e => e.GetProperty("op").GetInt32() is 10)
                .SelectMany(e => SendShardIdentityAsync(shard).ToObservable())
                .Subscribe();

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
    private async Task PublishDiscordEventAsync(JsonElement gatewayEvent)
    {
        var eventName = gatewayEvent.GetProperty("t").GetString();
        var eventJson = gatewayEvent.GetProperty("d");

        DiscordEvent eventObject;

        if (eventName is "READY")
        {
            var shardId = eventJson.GetProperty("shard").EnumerateArray().First().GetInt16();
            eventObject = new ConnectedEvent() { Shard = _shards[shardId] };

            foreach (var partialGuild in eventJson.GetProperty("guilds").EnumerateArray())
                _unavailableGuilds.Add(partialGuild.GetProperty("id").ToSnowflake());

            var user = new DiscordUser(this, eventJson.GetProperty("user"));
            if (this.Self.Id is null && this.Self.Id != user.Id)
                this.Self = user;

            this.UserCache.Add(user);
        }
        else if (eventName is "CHANNEL_CREATE")
        {
            var channel = eventJson.ToChannelEntity(this);
            eventObject = new EntityCreatedEvent<DiscordChannel>() { Entity = channel };

            this.ChannelCache.Add(channel);
        }
        else if (eventName is "CHANNEL_UPDATE")
        {
            var updatedChannel = eventJson.ToChannelEntity(this);
            var outdatedChannel = this.ChannelCache.GetAndUpdate(updatedChannel);

            eventObject = new EntityUpdatedEvent<DiscordChannel>()
            {
                UpdatedEntity = updatedChannel,
                OutdatedEnity = outdatedChannel
            };

            this.ChannelCache.Add(updatedChannel);
        }
        else if (eventName is "CHANNEL_DELETE")
        {
            var channelId = eventJson.GetProperty("id").ToSnowflake();
            this.ChannelCache.TryRemoveEntity(channelId, out var cachedChannel);

            eventObject = new EntityDeletedEvent<DiscordChannel>() 
            { 
                EntityId = channelId, 
                CachedEntity = cachedChannel 
            };
        }
        else if (eventName is "THREAD_CREATE")
        {
            var threadChannel = eventJson.ToChannelEntity(this) as DiscordThreadTextChannel;
            eventObject = new EntityCreatedEvent<DiscordThreadTextChannel>() { Entity = threadChannel };

            var parentChannel = await threadChannel.GetParentChannelAsync();
            parentChannel.ThreadCache.Add(threadChannel);
        }
        else if (eventName is "THREAD_UPDATE")
        {
            var updatedThread = eventJson.ToChannelEntity(this) as DiscordThreadTextChannel;
            var parentChannel = await updatedThread.GetParentChannelAsync();
            var outdatedThread = parentChannel.ThreadCache.GetAndUpdate(updatedThread);

            eventObject = new EntityUpdatedEvent<DiscordThreadTextChannel>()
            {
                UpdatedEntity = updatedThread,
                OutdatedEnity = outdatedThread
            };
        }
        else if (eventName is "THREAD_DELETE")
        {
            var threadId = eventJson.GetProperty("id").ToSnowflake();
            var parentId = eventJson.GetProperty("parent_id").ToSnowflake();
            var parentChannel = await GetChannelAsync<DiscordGuildTextChannel>(parentId);
            parentChannel.ThreadCache.(threadId, out DiscordThreadTextChannel cachedThread);

            eventObject = new EntityDeletedEvent<DiscordThreadTextChannel>() 
            { 
                EntityId = threadId, 
                CachedEntity = cachedThread 
            };
        }
        else if (eventName is "THREAD_LIST_SYNC")
        {
            foreach (var parentChannelId in eventJson.GetProperty("channel_ids").EnumerateArray().Select(json => json.ToSnowflake()))
            {
                var parentChannel = await GetChannelAsync<DiscordGuildTextChannel>(parentChannelId);
                var updatedThreads = eventJson.GetProperty("threads").EnumerateArray()
                    .Where(threadJson => threadJson.GetProperty("parent_id").ToSnowflake() == parentChannelId)
                    .Select(threadJson => threadJson.ToChannelEntity(this) as DiscordThreadTextChannel);

                parentChannel.ThreadCache.ReplaceAll(updatedThreads);
            }
        }
        else if (eventName is "THREAD_MEMBER_UPDATE")
        {

        }
        else
        {
            this.Logger.LogWarning("Received unknown gateway event: {EventName}", eventName);
            eventObject = new UnknownEvent() { Name = eventName, Json = eventJson };
        }

        eventObject.Bot = this;




        DiscordEvent eventObject = eventName switch
        {
            "WEBHOOKS_UPDATE" => throw new NotImplementedException(),
            "THREAD_LIST_SYNC" => throw new NotImplementedException(),

            "THREAD_MEMBER_UPDATE" => throw new NotImplementedException(),

            "GUILD_CREATE" => throw new NotImplementedException(),
            "GUILD_UPDATE" => throw new NotImplementedException(),
            "GUILD_DELETE" => throw new NotImplementedException(),
            "GUILD_BAN_ADD" => throw new NotImplementedException(),
            "GUILD_BAN_REMOVE" => throw new NotImplementedException(),
            "GUILD_EMOJIS_UPDATE" => throw new NotImplementedException(),
            "GUILD_STICKERS_UPDATE" => throw new NotImplementedException(),
            "GUILD_INTREGRATIONS_UPDATE" => throw new NotImplementedException(),
            "GUILD_MEMBER_ADD" => throw new NotImplementedException(),
            "GUILD_MEMBER_REMOVE" => throw new NotImplementedException(),
            "GUILD_MEMBER_UPDATE" => throw new NotImplementedException(),
            "GUILD_MEMBERS_CHUNK" => throw new NotImplementedException(),
            "GUILD_ROLE_CREATE" => throw new NotImplementedException(),
            "GUILD_ROLE_UPDATE" => throw new NotImplementedException(),
            "GUILD_ROLE_DELETE" => throw new NotImplementedException(),
            "GUILD_SCHEDULED_EVENT_CREATE" => throw new NotImplementedException(),
            "GUILD_SCHEDULED_EVENT_UPDATE" => throw new NotImplementedException(),
            "GUILD_SCHEDULED_EVENT_DELETE" => throw new NotImplementedException(),
            "GUILD_SCHEDULED_EVENT_USER_ADD" => throw new NotImplementedException(),
            "GUILD_SCHEDULED_EVENT_USER_REMOVE" => throw new NotImplementedException(),

            "INTEGRATION_CREATE" => throw new NotImplementedException(),
            "INTEGRATION_UPDATE" => throw new NotImplementedException(),
            "INTEGRATION_DELETE" => throw new NotImplementedException(),
            "INTERACTION_CREATE" => throw new NotImplementedException(),

            "INVITE_CREATE" => throw new NotImplementedException(),
            "INVITE_DELETE" => throw new NotImplementedException(),

            "MESSAGE_CREATE" => throw new NotImplementedException(),
            "MESSAGE_UPDATE" => throw new NotImplementedException(),
            "MESSAGE DELETE" => throw new NotImplementedException(),
            "MESSAGE_DELETE_BULK" => throw new NotImplementedException(),
            "MESSAGE_REACTION_ADD" => throw new NotImplementedException(),
            "MESSAGE_REACTION_REMOVE" => throw new NotImplementedException(),
            "MESSAGE_REACTION_REMOVE_EMOJI" => throw new NotImplementedException(),
            "TYPING_START" => throw new NotImplementedException(),

            "STAGE_INSTANCE_CREATE" => throw new NotImplementedException(),
            "STAGE_INSTANCE_UPDATE" => throw new NotImplementedException(),
            "STAGE_INSTANCE_DELETE" => throw new NotImplementedException(),

            "VOICE_STATE_UPDATE" => throw new NotImplementedException(),
            "VOICE_SERVER_UPDATE" => throw new NotImplementedException(),

            "PRESENSE_UPDATE" => throw new NotImplementedException(),
            "USER_UPDATE" => throw new NotImplementedException(),

            _ => UnknownEvent()
        };

        _eventSequence.OnNext(eventObject);
    }
}
