namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Extension.Internal;
using Entity;
using Enum;
using Event;
using Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rest.Extension;
using Rest.Extension.Endpoint;
using Type;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through one or more websocket connections.
/// Sends requests to the API through HTTP REST or a websocket connection.
/// </remarks>
public sealed class GatewayBot : Bot
{
    private GatewayIntent _intents;
    private WebsocketShard[] _shards;
    private Subject<BotEvent> _events;
    private List<Snowflake> _unknownGuilds;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="loggerFactory"></param>
    public GatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILoggerFactory loggerFactory = null)
        : base(token, loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _shards = Array.Empty<WebsocketShard>();
        _events = new Subject<BotEvent>();
        _unknownGuilds = new List<Snowflake>();

        this.DirectMessageChannelCache = new EntityCache<DirectMessageChannel>();
    }

    /// <summary></summary>
    public ReadOnlyCollection<WebsocketShard> Shards => new(_shards);

    /// <summary>An observable sequence of events received from each shard.</summary>
    public IObservable<BotEvent> Events => _events.AsObservable();

    /// <summary>Whether this instance has an active websocket connection.</summary>
    public override bool IsConnected => _shards.Length > 0 && _shards.All(shard => shard.IsConnected);

    /// <summary>Average websocket latency across all shards.</summary>
    public TimeSpan Latency => TimeSpan.FromTicks(Convert.ToInt64(_shards.Average(shard => shard.Latency.Ticks)));

    /// <summary>Cached direct message channels.</summary>
    public EntityCache<DirectMessageChannel> DirectMessageChannelCache { get; private init; }

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
                .SubscribeAsync(payload => this.IdentifyAsync(shard).AsTask());

            shard.Payloads.Where(payload => payload.GetProperty("op").GetInt32() is 0)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Subscribe(this.EventHandler);

            _shards[shardId] = shard;
        }

        for (int index = 0; index < shardCount; index += clusterSize)
        {
            var cluster = _shards.Skip(index).Take(clusterSize);
            foreach (var shard in cluster) await shard.ConnectAsync();
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

    /// <summary>Fetches a user object for the account associated with the token provided during construction of this instance.</summary>
    public async Task<User> GetSelfAsync()
    {
        var json = await this.RestClient.GetSelfAsync();
        var user = new User(this, json);
        this.UserCache.Add(user);

        return user;
    }

    private ValueTask IdentifyAsync(WebsocketShard shard)
    {
        if (shard.SessionId is null)
            return shard.SendPayloadAsync(2, json =>
            {
                json.WriteString("token", this.Token);

                json.WriteStartObject("properties");
                json.WriteString("os", Environment.OSVersion.ToString());
                json.WriteString("browser", "Donatello/0.0.0");
                json.WriteString("device", "Donatello/0.0.0");
                json.WriteEndObject();

                json.WriteStartArray("shard");
                json.WriteNumberValue(shard.Id);
                json.WriteNumberValue(_shards.Length);
                json.WriteEndArray();

                json.WriteNumber("intents", (int)_intents);
                json.WriteNumber("large_threshold", 250);
                // json.WriteBoolean("compress", true);
            });
        else
            return shard.SendPayloadAsync(6, json =>
            {
                json.WriteString("token", this.Token);
                json.WriteString("session_id", shard.SessionId);
                json.WriteNumber("seq", shard.EventIndex);
            });
    }

    /// <summary>Processes the provided gateway event and notifies subscribers of <see cref="Events"/> about the event.</summary>
    private void EventHandler(JsonElement eventPayload)
    {
        var eventJson = eventPayload.GetProperty("d");
        var eventName = eventPayload.GetProperty("t").GetString();

        switch (eventName)
        {
            case "READY":
            {
                var self = new User(this, eventJson.GetProperty("user"));
                var guildIds = eventJson.GetProperty("guilds").EnumerateArray()
                    .Select(json => json.GetProperty("id").ToSnowflake())
                    .ToList();
                var connectedEvent = new ConnectedEvent { GuildIds = guildIds.AsReadOnly() };

                _unknownGuilds.AddRange(guildIds);
                this.UserCache.Add(self);

                _events.OnNext(connectedEvent);
                break;
            }
            case "CHANNEL_CREATE":
            {
                var channel = UpdateOrCreateChannel(eventJson);

                if (channel is IGuildChannel guildChannel && this.GuildCache.TryGet(guildChannel.GuildId, out Guild guild))
                    guild.ChannelCache.Add(guildChannel);

                if (channel is DirectMessageChannel dmChannel)
                {
                    this.DirectMessageChannelCache.Add(dmChannel);
                    EntityCreated(dmChannel);
                }
                else if (channel is GuildNewsChannel newsChannel)
                    EntityCreated(newsChannel);
                else if (channel is GuildTextChannel textChannel)
                    EntityCreated(textChannel);
                else if (channel is GuildVoiceChannel voiceChannel)
                    EntityCreated(voiceChannel);
                else if (channel is GuildCategoryChannel categoryChannel)
                    EntityCreated(categoryChannel);
                else if (channel is GuildForumChannel forumChannel)
                    EntityCreated(forumChannel);
                else
                    Unknown();

                break;
            }
            case "CHANNEL_UPDATE":
            {
                var updatedChannel = Channel.Create(this, eventJson);
                IChannel outdatedChannel = null;

                if (updatedChannel is IGuildChannel guildChannel && this.GuildCache.TryGet(guildChannel.GuildId, out Guild guild))
                    outdatedChannel = guild.ChannelCache.TryUpdate(guildChannel);

                if (updatedChannel is DirectMessageChannel dmChannel)
                {
                    outdatedChannel = this.DirectMessageChannelCache.TryUpdate(dmChannel);
                    _events.OnNext(new EntityCreatedEvent<DirectMessageChannel> { Entity = dmChannel });
                }
                else if (updatedChannel is GuildNewsChannel newsChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildNewsChannel> { Entity = newsChannel });
                else if (updatedChannel is GuildTextChannel textChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildTextChannel> { Entity = textChannel });
                else if (updatedChannel is GuildVoiceChannel voiceChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildVoiceChannel> { Entity = voiceChannel });
                else if (updatedChannel is GuildCategoryChannel categoryChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildCategoryChannel> { Entity = categoryChannel });
                else if (updatedChannel is GuildForumChannel forumChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildForumChannel> { Entity = forumChannel });

                break;
            }
            case "CHANNEL_DELETE":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var channelId = eventJson.GetProperty("id").ToSnowflake();
                IChannel channelInstance;

                if (this.GuildCache.TryGet(guildId, out Guild guild) && guild.ChannelCache.Contains(channelId))
                    channelInstance = guild.ChannelCache.Remove(channelId);
                else if (this.DirectMessageChannelCache.Contains(channelId))
                    channelInstance = this.DirectMessageChannelCache.Remove(channelId);
                else
                    channelInstance = Channel.Create(this, eventJson);

                if (channelInstance is DirectMessageChannel dmChannel)
                    _events.OnNext(new EntityCreatedEvent<DirectMessageChannel> { Entity = dmChannel });
                else if (channelInstance is GuildNewsChannel newsChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildNewsChannel> { Entity = newsChannel });
                else if (channelInstance is GuildTextChannel textChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildTextChannel> { Entity = textChannel });
                else if (channelInstance is GuildVoiceChannel voiceChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildVoiceChannel> { Entity = voiceChannel });
                else if (channelInstance is GuildCategoryChannel categoryChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildCategoryChannel> { Entity = categoryChannel });
                else if (channelInstance is GuildForumChannel forumChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildForumChannel> { Entity = forumChannel });

                break;
            }
            case "THREAD_CREATE":
            {
                var channel = Channel.Create<IThreadChannel>(this, eventJson);

                if (this.GuildCache.TryGet(channel.GuildId, out Guild cachedGuild) &&
                    cachedGuild.ChannelCache.TryGet(channel.ParentId, out IGuildChannel cachedChannel))
                {
                    if (cachedChannel is GuildTextChannel textChannel)
                        textChannel.ThreadCache.Add(channel as GuildThreadChannel);
                    else if (cachedChannel is GuildForumChannel forumChannel)
                        forumChannel.PostCache.Add(channel as ForumPostChannel);
                }

                if (channel is GuildThreadChannel threadChannel)
                    _events.OnNext(new EntityCreatedEvent<GuildThreadChannel> { Entity = threadChannel });
                else if (channel is ForumPostChannel postChannel)
                    _events.OnNext(new EntityCreatedEvent<ForumPostChannel> { Entity = postChannel });
                else
                    Unknown();

                break;
            }
            case "THREAD_DELETE":
            {
                IThreadChannel channel = null;
                var threadId = eventJson.GetProperty("id").ToSnowflake();
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();

                if (this.GuildCache.TryGet(guildId, out Guild cachedGuild))
                {
                    var parentId = eventJson.GetProperty("parent_id").ToSnowflake();
                    if (cachedGuild.ChannelCache.TryGet(parentId, out IGuildChannel cachedChannel))
                    {
                        if (cachedChannel is GuildTextChannel textChannel)
                            channel = textChannel.ThreadCache.Remove(threadId);
                    }
                }

                EntityDeletedEvent<T> ThreadDeletedEvent<T>(Snowflake entityId, T cachedInstance) where T : class, IThreadChannel
                    => new() { EntityId = entityId, Instance = cachedInstance };

                _events.OnNext(ThreadDeletedEvent(threadId, channel));
                break;
            }
            case "THREAD_LIST_SYNC":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var threads = eventJson.GetProperty("threads").EnumerateArray()
                    .Select(json => Channel.Create<GuildThreadChannel>(this, json))
                    .ToList();
                var members = eventJson.GetProperty("members").EnumerateArray();

                // Add members to respective thread member cache.
                foreach (var thread in threads)
                    foreach (var member in members.Where(memberJson => thread.Id == memberJson.GetProperty("id").ToSnowflake()))
                        thread.MemberCache.Add(member.GetProperty("user_id").ToSnowflake(), member);

                // Update thread caches.
                if (this.GuildCache.TryGet(guildId, out Guild guild))
                {
                    if (eventJson.TryGetProperty("channel_ids", out JsonElement snowflakeArray) && snowflakeArray.GetArrayLength() is not 0)
                    {
                        // Clear thread cache for any channel IDs did not have a thread sent with this event.
                        var channelIds = snowflakeArray.EnumerateArray()
                            .Select(json => json.ToSnowflake())
                            .Where(id => threads.All(thread => id != thread.ParentId));

                        var inactiveChannels = guild.ChannelCache
                            .OfType<GuildTextChannel>()
                            .Where(channel => channelIds.Contains(channel.Id));

                        foreach (var textChannel in inactiveChannels)
                            textChannel.ThreadCache.Clear();
                    }
                    else
                    {
                        // Clear thread cache for all channels.
                        foreach (var cachedGuild in this.GuildCache)
                            foreach (var cachedChannel in cachedGuild.ChannelCache.OfType<GuildTextChannel>())
                                cachedChannel.ThreadCache.Clear();
                    }

                    // Add new threads to respective thread caches.
                    foreach (var thread in threads)
                        if (guild.ChannelCache.TryGet(thread.ParentId, out IGuildChannel guildChannel))
                            if (guildChannel is GuildTextChannel textChannel)
                                textChannel.ThreadCache.Add(thread);
                }

                foreach (var thread in threads)
                    _events.OnNext(new EntityCreatedEvent<GuildThreadChannel> { Entity = thread });

                break;
            }
            case "CHANNEL_PINS_UPDATE":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var channelId = eventJson.GetProperty("channel_id").ToSnowflake();

                _events.OnNext(new ChannelPinsUpdatedEvent { ChannelId = channelId, GuildId = guildId });
                break;
            }
            case "GUILD_CREATE":
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

                var guild = new Guild(this, mutableJson.AsElement());

                foreach (var memberJson in members.AsArray())
                {
                    var userId = memberJson["user"]["id"].AsValue().ToSnowflake();
                    guild.MemberCache.Add(userId, memberJson.AsElement());
                }

                foreach (var channelJson in channels.AsArray().Select(node => node.AsObject()))
                {
                    var channel = Channel.Create<IGuildChannel>(this, channelJson, guild.Id);
                    guild.ChannelCache.Add(channel);
                }

                foreach (var threadJson in threads.AsArray().Select(node => node.AsObject()))
                {
                    var thread = Channel.Create<GuildThreadChannel>(this, threadJson);
                    var parent = guild.ChannelCache[thread.ParentId] as GuildTextChannel;

                    parent.ThreadCache.Add(thread);
                }

                foreach (var stateJson in voiceStates.AsArray().Select(node => node.AsObject()))
                {
                    var userId = stateJson["user_id"].AsValue().ToSnowflake();
                    var voiceState = new DiscordVoiceState(this, stateJson.AsElement());

                    guild.VoiceStateCache.Add(userId, voiceState);
                }

                // TODO : user presences

                // TODO : stages

                // TODO : events

                this.GuildCache.Add(guild);
                _events.OnNext(new EntityCreatedEvent<Guild> { Entity = guild });
                break;
            }
            case "GUILD_UPDATE":
            {
                var updatedGuild = new Guild(this, eventPayload.GetProperty("d"));
                var outdatedGuild = this.GuildCache.TryUpdate(updatedGuild);

                _events.OnNext(new EntityUpdatedEvent<Guild> { UpdatedEntity = updatedGuild, OutdatedEntity = outdatedGuild });
                break;
            }
            case "GUILD_DELETE":
            {
                var guildId = eventJson.ToSnowflake();
                _events.OnNext(new EntityDeletedEvent<Guild> { EntityId = guildId, Instance = this.GuildCache.Remove(guildId) });
                break;
            }
            case "GUILD_AUDIT_LOG_ENTRY_CREATE":
            {
                break;
            }
            case "GUILD_BAN_ADD":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var user = new User(this, eventJson.GetProperty("user"));

                this.UserCache.Add(user);

                _events.OnNext(new GuildBanEvent { GuildId = guildId, User = user });
                break;
            }
            case "GUILD_BAN_REMOVE":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var user = new User(this, eventJson.GetProperty("user"));

                this.UserCache.Add(user);

                _events.OnNext(new GuildUnbanEvent() { GuildId = guildId, User = user });
                break;
            }
            case "GUILD_EMOJIS_UPDATE":
            {
                break;
            }
            default:
                Unknown();
                break;
        }

        void EntityCreated<TEntity>(TEntity entity) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityCreatedEvent<TEntity> { Entity = entity });

        void EntityUpdated<TEntity>(TEntity updated, TEntity outdated) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityUpdatedEvent<TEntity> { UpdatedEntity = updated, OutdatedEntity = outdated });

        void EntityDeleted<TEntity>(Snowflake snowflake, TEntity instance = null) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityDeletedEvent<TEntity> { EntityId = snowflake, Instance = instance });

        void Unknown()
            => _events.OnNext(new UnknownEvent() { Name = eventName, Json = eventJson });

        IChannel UpdateOrCreateChannel(JsonElement channelJson)
        {
            var channelId = channelJson.GetProperty("id").ToSnowflake();
            var channelType = channelJson.GetProperty("type").GetInt32();
            IChannel cachedInstance = null;

            if (channelType is 1 or 3)
            {
                if (this.DirectMessageChannelCache.TryGet(channelId, out DirectMessageChannel dmChannel))
                    dmChannel.Update(channelJson);
                else
                {
                    dmChannel = Channel.Create<DirectMessageChannel>(this, channelJson);
                    this.DirectMessageChannelCache.Add(dmChannel);
                }

                cachedInstance = dmChannel;
            }
            else if (channelJson.TryGetProperty("guild_id", out JsonElement snowflakeJson) &&
                     this.GuildCache.TryGet(snowflakeJson.ToSnowflake(), out Guild guild))
            {
                if (channelType is 10 or 11 or 12)
                {
                    var parentId = channelJson.GetProperty("parent_id").ToSnowflake();
                    if (guild.ChannelCache.TryGet(parentId, out IGuildChannel parentChannel))
                    {
                        
                    }
                }
                else if (guild.ChannelCache.TryGet(channelId, out IGuildChannel guildChannel))
                    guildChannel.Update(channelJson);
                else
                {
                    guildChannel = Channel.Create<IGuildChannel>(this, channelJson);
                    guild.ChannelCache.Add(guildChannel);
                }

                cachedInstance = guildChannel;
            }

            return cachedInstance ?? Channel.Create(this, channelJson);
        }
    }
}