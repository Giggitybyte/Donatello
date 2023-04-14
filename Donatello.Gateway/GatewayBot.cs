namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Common;
using Common.Entity.Channel;
using Common.Entity.Guild;
using Common.Entity.Guild.Channel;
using Common.Entity.User;
using Common.Entity.Voice;
using Common.Enum;
using Common.Extension;
using Event;
using Microsoft.Extensions.Logging;
using Rest.Extension;
using Rest.Extension.Endpoint;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through one or more websocket connections.
/// Sends requests to the API through HTTP REST or a websocket connection.
/// </remarks>
public sealed class GatewayBot : Bot
{
    private GatewayIntent _intents;
    private WebsocketShard[] _shards;
    private Subject<IEvent> _eventSubject;
    private List<Snowflake> _unknownGuilds;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="loggerFactory"></param>
    public GatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILoggerFactory loggerFactory = null) : base(token, loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token cannot be empty.", nameof(token));

        _intents = intents;
        _shards = Array.Empty<WebsocketShard>();
        _eventSubject = new Subject<IEvent>();
        _unknownGuilds = new List<Snowflake>();
    }

    /// <summary>Active websocket connections.</summary>
    public ReadOnlyCollection<WebsocketShard> Shards => new(_shards);

    /// <summary>An observable sequence of events received from each shard.</summary>
    public IObservable<IEvent> Events => _eventSubject.AsObservable();

    /// <summary>Whether this instance has an active websocket connection.</summary>
    public override bool IsConnected => _shards.Length > 0 && _shards.All(shard => shard.IsConnected);

    /// <summary>Average websocket latency across all shards.</summary>
    public TimeSpan Latency
    {
        get
        {
            if (_shards.Length is 1) return _shards[0].Latency;

            double averageTicks = _shards.Average(shard => shard.Latency.Ticks);
            long roundedAverage = Convert.ToInt64(averageTicks);
            return TimeSpan.FromTicks(roundedAverage);
        }
    }

    /// <summary>Connects to the Discord gateway.</summary>
    public override async Task StartAsync()
    {
        var websocketMetadata = await this.RestClient.SendRequestAsync(HttpMethod.Get, "gateway/bot").GetJsonAsync();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var clusterSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new WebsocketShard[shardCount];

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new WebsocketShard(shardId, this.LoggerFactory.CreateLogger($"Websocket Shard {shardId}"));

            shard.Payloads.Where(payload => payload.GetProperty("op").GetInt32() is 10)
                .Subscribe(payload => shard.IdentifyAsync(this.Token, _intents, shardCount));

            shard.Payloads.Where(payload => payload.GetProperty("op").GetInt32() is 0)
                .Subscribe(this.EventHandler);

            _shards[shardId] = shard;
        }

        for (int index = 0; index < shardCount; index += clusterSize)
        {
            var connectTasks = _shards.Skip(index).Take(clusterSize).Select(shard => shard.ConnectAsync());
            await Task.WhenAll(connectTasks);
            if (index < clusterSize) await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>Closes all websocket connections.</summary>
    public override async Task StopAsync()
    {
        if (this.IsConnected is false)
            throw new InvalidOperationException("This instance is not currently connected to Discord.");

        var disconnectTasks = new Task[_shards.Length];

        foreach (var shard in _shards)
            disconnectTasks[shard.Id] = shard.DisconnectAsync();

        await Task.WhenAll(disconnectTasks);
        _shards = Array.Empty<WebsocketShard>();
    }

    /// <summary>Fetches the user associated with the token provided during the creation of this instance.</summary>
    public async Task<User> GetSelfAsync()
    {
        var userJson = await this.RestClient.GetSelfAsync();
        this.UserCache.AddOrUpdate(userJson);

        return new User(userJson, this);
    }

    /// <summary>Processes the provided gateway event and notifies subscribers to <see cref="Events"/> about the event.</summary>
    private void EventHandler(JsonElement eventPayload)
    {
        var eventJson = eventPayload.GetProperty("d");
        var eventName = eventPayload.GetProperty("t").GetString();
        var eventObjects = new List<IEvent>();

        if (eventName == "READY") 
            ConnectedEvent();
        else if (eventName == "CHANNEL_CREATE")
            ChannelCreatedEvent();
        else if (eventName == "CHANNEL_UPDATE")
            ChannelUpdatedEvent();
        else if (eventName == "CHANNEL_DELETE")
            ChannelDeletedEvent();
        else if (eventName == "THREAD_CREATE")
            ThreadCreatedEvent();
        else if (eventName == "THREAD_UPDATE")
            ThreadUpdatedEvent();
        else if (eventName == "THREAD_DELETE")
            ThreadDeletedEvent();
        else if (eventName == "THREAD_LIST_SYNC")
            ThreadSyncEvent();
        else if (eventName == "CHANNEL_PINS_UPDATE")
            PinsUpdatedEvent();
        else if (eventName == "GUILD_CREATE")
            GuildCreatedEvent();
        else if (eventName == "GUILD_UPDATE")
            GuildUpdatedEvent();
        else if (eventName == "GUILD_DELETE")
            GuildDeletedEvent();
        else if (eventName == "GUILD_AUDIT_LOG_ENTRY_CREATE")
            GuildAuditLogEvent();
        else if (eventName == "GUILD_BAN_ADD")
            GuildBanEvent();
        else if (eventName == "GUILD_BAN_REMOVE")
            GuildUnbanEvent();
        else
            UnknownEvent();

        foreach (var eventObject in eventObjects)
            _eventSubject.OnNext(eventObject);

        
        void ConnectedEvent()
        {
            var guildIds = eventJson.GetProperty("guilds").EnumerateArray()
                .Select(json => json.GetProperty("id").ToSnowflake())
                .ToList();

            _unknownGuilds.AddRange(guildIds);
            this.UserCache.AddOrUpdate(eventJson.GetProperty("user"));

            eventObjects.Add(new ConnectedEvent { GuildIds = guildIds.AsReadOnly() });
        }

        void ChannelCreatedEvent()
        {
            this.ChannelCache.AddOrUpdate(eventJson);
            eventObjects.Add(EntityCreatedEvent.Create(eventJson.AsChannel(this)));
        }

        void ChannelUpdatedEvent()
        {
            IChannel updatedChannel = eventJson.AsChannel(this);
            JsonElement outdatedJson = this.ChannelCache.AddOrUpdate(eventJson);
            IChannel outdatedChannel = outdatedJson.AsChannel(this);

            eventObjects.Add(EntityUpdatedEvent.Create(updatedChannel, outdatedChannel));
        }

        void ChannelDeletedEvent()
        {
            var deletedChannel = eventJson.AsChannel(this);
            this.ChannelCache.RemoveEntry(deletedChannel.Id);

            eventObjects.Add(EntityDeletedEvent.Create(deletedChannel));
        }

        void ThreadCreatedEvent()
        {
            var newThread = eventJson.AsChannel<GuildThreadChannel>(this);
            this.GuildThreadCache[newThread.GuildId].AddOrUpdate(newThread.Json);

            eventObjects.Add(EntityCreatedEvent.Create(newThread));
        }

        void ThreadUpdatedEvent()
        {
            var updatedThread = eventJson.AsChannel<GuildThreadChannel>(this);
            var outdatedJson = this.GuildThreadCache[updatedThread.GuildId].AddOrUpdate(eventJson);
            var outdatedThread = outdatedJson.AsChannel<GuildThreadChannel>(this);

            eventObjects.Add(EntityUpdatedEvent.Create(updatedThread, outdatedThread));
        }

        void ThreadDeletedEvent()
        {
            var threadId = eventJson.GetProperty("id").ToSnowflake();
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
            var cachedJson = this.GuildThreadCache[guildId].RemoveEntry(threadId);
            var cachedThread = cachedJson.AsChannel<GuildThreadChannel>(this);

            eventObjects.Add(EntityDeletedEvent.Create(threadId, cachedThread));
        }

        void ThreadSyncEvent()
        {
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();

            if (eventJson.TryGetProperty("channel_ids", out JsonElement snowflakes) is false || snowflakes.GetArrayLength() is 0)
                this.GuildThreadCache[guildId].Clear();

            foreach (var threadJson in eventJson.GetProperty("threads").EnumerateArray())
            {
                var threadChannel = threadJson.AsChannel<GuildThreadChannel>(this);
                this.GuildThreadCache[guildId].AddOrUpdate(threadJson);
                eventObjects.Add(EntityCreatedEvent.Create(threadChannel));
            }

            foreach (var memberJson in eventJson.GetProperty("members").EnumerateArray())
            {
                var threadMember = new ThreadMember(memberJson, this);
                this.ThreadMemberCache[threadMember.ThreadId].AddOrUpdate(threadMember.Id, threadMember.Json);
                eventObjects.Add(EntityCreatedEvent.Create(threadMember));
            }
        }

        void PinsUpdatedEvent()
        {
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
            var channelId = eventJson.GetProperty("channel_id").ToSnowflake();

            eventObjects.Add(new PinsUpdatedEvent { ChannelId = channelId, GuildId = guildId });
        }

        void GuildCreatedEvent()
        {
            var mutableJson = JsonObject.Create(eventJson)!;

            mutableJson.Remove("large");
            mutableJson.Remove("member_count");
            mutableJson.Remove("joined_at");

            mutableJson.Remove("members", out JsonNode members);
            mutableJson.Remove("channels", out JsonNode channels);
            mutableJson.Remove("threads", out JsonNode threads);
            mutableJson.Remove("voice_states", out JsonNode voiceStates);
            mutableJson.Remove("presences", out JsonNode presences);
            mutableJson.Remove("stage_instances", out JsonNode stageInstances);
            mutableJson.Remove("guild_scheduled_events", out JsonNode scheduledEvents);

            var guild = new Guild(mutableJson.AsElement(), this);
            this.GuildCache.AddOrUpdate(guild.Json);
            eventObjects.Add(EntityCreatedEvent.Create(guild));

            foreach (var memberJson in members.AsArray())
            {
                var member = new GuildMember(memberJson.AsElement(), guild.Id, this);
                this.GuildMemberCache[guild.Id].AddOrUpdate(member.Id, member.Json);
                eventObjects.Add(EntityCreatedEvent.Create(member));
            }

            foreach (var channelJson in channels.AsArray().Select(node => node.AsObject()))
            {
                IChannel channel = channelJson.AsChannel(guild.Id, this);
                this.ChannelCache.AddOrUpdate(channel.Json);
                eventObjects.Add(EntityCreatedEvent.Create(channelJson.AsChannel(guild.Id, this)));
            }

            foreach (var threadJson in threads.AsArray().Select(node => node.AsObject()))
            {
                threadJson.Remove("member", out JsonNode memberJson);
                var thread = threadJson.AsChannel<GuildThreadChannel>(guild.Id, this);
                this.GuildThreadCache[guild.Id].AddOrUpdate(thread.Json);

                eventObjects.Add(EntityCreatedEvent.Create(thread));
                if (memberJson is not null)
                {
                    var threadMember = new ThreadMember(memberJson.AsElement(), this);
                    this.ThreadMemberCache[thread.Id].AddOrUpdate(threadMember.Json);
                    eventObjects.Add(EntityCreatedEvent.Create(threadMember));
                }
            }

            foreach (var stateJson in voiceStates.AsArray().Select(node => node.AsObject()))
            {
                var userId = stateJson["user_id"].AsValue().ToSnowflake();
                var voiceState = new DiscordVoiceState(this, stateJson.AsElement());
                this.VoiceStateCache.AddOrUpdate(userId, voiceState.Json);
            }

            // TODO : user presences

            // TODO : stages

            // TODO : events
        }

        void GuildUpdatedEvent()
        {
            var updatedGuild = new Guild(eventJson, this);
            var outdatedJson = this.GuildCache.AddOrUpdate(updatedGuild.Json);
            var outdatedGuild = new Guild(outdatedJson, this);
            eventObjects.Add(EntityUpdatedEvent.Create(updatedGuild, outdatedGuild));
        }

        void GuildDeletedEvent()
        {
            var guildId = eventJson.ToSnowflake();
            var cachedJson = this.GuildCache.RemoveEntry(guildId);
            Guild cachedGuild = cachedJson.ValueKind is JsonValueKind.Object ? new Guild(cachedJson, this) : null;

            eventObjects.Add(EntityDeletedEvent.Create(guildId, cachedGuild));
        }

        void GuildAuditLogEvent()
            => this.Logger.LogWarning("You haven't implemented GUILD_AUDIT_LOG_ENTRY_CREATE, nerd");

        void GuildBanEvent()
        {
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
            var user = new User(eventJson.GetProperty("user"), this);
            this.UserCache.AddOrUpdate(user.Json);

            eventObjects.Add(new BanEvent { GuildId = guildId, User = user });
        }

        void GuildUnbanEvent()
        {
            var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
            var user = new User(eventJson.GetProperty("user"), this);
            this.UserCache.AddOrUpdate(user.Json);
            
            eventObjects.Add(new UnbanEvent() { GuildId = guildId, User = user });
        }

        void UnknownEvent()
        {
            this.Logger.LogDebug("Received unknown event '{Name}'. Do you have the latest version of this library?", eventName);
            eventObjects.Add(new UnknownEvent { Name = eventName, Json = eventJson });
        }
    }
}