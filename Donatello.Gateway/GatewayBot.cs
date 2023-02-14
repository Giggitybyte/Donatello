﻿namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Common;
using Common.Entity;
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
    public TimeSpan Latency => TimeSpan.FromTicks
    (
        Convert.ToInt64(_shards.Average(shard => shard.Latency.Ticks))
    );

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
                .SubscribeAsync(payload => this.IdentifyAsync(shard));

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

    private Task IdentifyAsync(WebsocketShard shard)
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
            }).AsTask();
        else
            return shard.SendPayloadAsync(6, json =>
            {
                json.WriteString("token", this.Token);
                json.WriteString("session_id", shard.SessionId);
                json.WriteNumber("seq", shard.EventIndex);
            }).AsTask();
    }

    /// <summary>Processes the provided gateway event and notifies subscribers of <see cref="Events"/> about the event.</summary>
    private void EventHandler(JsonElement eventPayload)
    {
        var eventJson = eventPayload.GetProperty("d");
        var eventName = eventPayload.GetProperty("t").GetString();

        void OnEntityCreated<TEntity>(TEntity entity) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityCreatedEvent<TEntity> { Entity = entity });

        void OnEntityUpdated<TEntity>(TEntity updated, TEntity outdated) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityUpdatedEvent<TEntity> { UpdatedEntity = updated, OutdatedEntity = outdated });

        void OnEntityDeleted<TEntity>(Snowflake snowflake, TEntity instance = null) where TEntity : class, ISnowflakeEntity
            => _events.OnNext(new EntityDeletedEvent<TEntity> { EntityId = snowflake, Instance = instance });

        void OnUnknownEvent()
            => _events.OnNext(new UnknownEvent() { Name = eventName, Json = eventJson });

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

                if (channel is GuildNewsChannel newsChannel)
                    OnEntityCreated(newsChannel);
                else if (channel is GuildTextChannel textChannel)
                    OnEntityCreated(textChannel);
                else if (channel is GuildVoiceChannel voiceChannel)
                    OnEntityCreated(voiceChannel);
                else if (channel is GuildCategoryChannel categoryChannel)
                    OnEntityCreated(categoryChannel);
                else if (channel is GuildForumChannel forumChannel)
                    OnEntityCreated(forumChannel);
                else if (channel is DirectMessageChannel dmChannel)
                    OnEntityCreated(dmChannel);
                else
                    OnUnknownEvent();

                break;
            }
            case "CHANNEL_UPDATE":
            {
                var updatedChannel = UpdateOrCreateChannel(eventJson);
                IChannel outdatedChannel = null;

                if (updatedChannel is IGuildChannel guildChannel && this.GuildCache.TryGet(guildChannel.GuildId, out Guild guild))
                    outdatedChannel = guild.ChannelCache.Add(guildChannel);

                if (updatedChannel is GuildNewsChannel newsChannel)
                    OnEntityUpdated(newsChannel, outdatedChannel);
                else if (updatedChannel is GuildTextChannel textChannel)
                    OnEntityUpdated(textChannel, outdatedChannel);
                else if (updatedChannel is GuildVoiceChannel voiceChannel)
                    OnEntityUpdated(voiceChannel, outdatedChannel);
                else if (updatedChannel is GuildCategoryChannel categoryChannel)
                    OnEntityUpdated(categoryChannel, outdatedChannel);
                else if (updatedChannel is GuildForumChannel forumChannel)
                    OnEntityUpdated(forumChannel, outdatedChannel);
                else if (updatedChannel is DirectMessageChannel dmChannel)
                {
                    outdatedChannel = this.DirectMessageChannelCache.Add(dmChannel);
                    OnEntityUpdated(dmChannel, outdatedChannel);
                }
                else
                    OnUnknownEvent();

                break;
            }
            case "CHANNEL_DELETE":
            {
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var channelId = eventJson.GetProperty("id").ToSnowflake();
                IChannel channelInstance = null;

                if (this.GuildCache.TryGet(guildId, out Guild guild) && guild.ChannelCache.TryRemove(channelId, out IGuildChannel guildChannel))
                    channelInstance = guildChannel;
                else if (this.DirectMessageChannelCache.TryRemove(channelId, out DirectMessageChannel directMessageChannel))
                    channelInstance = directMessageChannel;
                
                if (channelInstance is null)
                    channelInstance = Channel.Create(this, eventJson);
                else 
                    channelInstance.Update(eventJson);

                if (channelInstance is DirectMessageChannel dmChannel)
                    OnEntityDeleted(channelId, dmChannel);
                else if (channelInstance is GuildNewsChannel newsChannel)
                    OnEntityDeleted(channelId, newsChannel);
                else if (channelInstance is GuildTextChannel textChannel)
                    OnEntityDeleted(channelId, textChannel);
                else if (channelInstance is GuildVoiceChannel voiceChannel)
                    OnEntityDeleted(channelId, voiceChannel);
                else if (channelInstance is GuildCategoryChannel categoryChannel)
                    OnEntityDeleted(channelId, categoryChannel);
                else if (channelInstance is GuildForumChannel forumChannel)
                    OnEntityDeleted(channelId, forumChannel);

                break;
            }
            case "THREAD_CREATE":
            {
                var threadChannel = UpdateOrCreateChannel(eventJson);

                OnEntityCreated(threadChannel);
                break;
            }
            case "THREAD_DELETE":
            {
                var threadId = eventJson.GetProperty("id").ToSnowflake();
                var parentId = eventJson.GetProperty("parent_id").ToSnowflake();
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();

                if (this.GuildCache.TryGet(guildId, out Guild guild) &&
                    guild.ChannelCache.TryGet(parentId, out IGuildChannel guildChannel) &&
                    guildChannel is GuildTextChannel textChannel &&
                    textChannel.ThreadCache.TryRemove(threadId, out GuildThreadChannel threadChannel))
                {
                    OnEntityDeleted(threadId, threadChannel);
                }
                else
                    OnEntityDeleted<GuildThreadChannel>(threadId);

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
                        // Clear thread cache for any channel IDs which did not have a thread sent with this event.
                        var channelIds = snowflakeArray.EnumerateArray()
                            .Select(json => json.ToSnowflake())
                            .Where(id => threads.All(thread => thread.ParentId != id));

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
                OnEntityCreated(guild);
                break;
            }
            case "GUILD_UPDATE":
            {
                var updatedGuild = new Guild(this, eventPayload.GetProperty("d"));
                var outdatedGuild = this.GuildCache.Add(updatedGuild);
                OnEntityUpdated(updatedGuild, outdatedGuild);

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
                OnUnknownEvent();
                break;
        }

        IChannel UpdateOrCreateChannel(JsonElement channelJson) // this doesn't work for returning old objects!!
        {
            var channelId = channelJson.GetProperty("id").ToSnowflake();
            var channelType = channelJson.GetProperty("type").GetInt32();
            IChannel instance = null;

            if (channelType is 1 or 3)
            {
                if (this.DirectMessageChannelCache.TryGet(channelId, out DirectMessageChannel dmChannel))
                    dmChannel.Update(channelJson);
                else
                {
                    dmChannel = Channel.Create<DirectMessageChannel>(this, channelJson);
                    this.DirectMessageChannelCache.Add(dmChannel);
                }

                instance = dmChannel;
            }
            else if (channelJson.TryGetProperty("guild_id", out JsonElement snowflakeJson) && this.GuildCache.TryGet(snowflakeJson.ToSnowflake(), out Guild guild))
            {
                IGuildChannel guildChannel = null;
                
                if (channelType is 10 or 11 or 12)
                {
                    var parentId = channelJson.GetProperty("parent_id").ToSnowflake();
                    if (guild.ChannelCache.TryGet(parentId, out IGuildChannel parentChannel))
                    {
                        GuildThreadChannel threadChannel = null;
                        
                        if (parentChannel is GuildTextChannel textChannel && textChannel.ThreadCache.TryGet(channelId, out threadChannel))
                            threadChannel.Update(channelJson);
                        else if (parentChannel is GuildForumChannel forumChannel && forumChannel.ThreadCache.TryGet(channelId, out threadChannel))
                            threadChannel.Update(channelJson); // hmm...

                        guildChannel = threadChannel;
                    }
                }
                else if (guild.ChannelCache.TryGet(channelId, out guildChannel))
                    guildChannel.Update(channelJson);
                
                if (guildChannel is null)
                {
                    guildChannel = Channel.Create<IGuildChannel>(this, channelJson);
                    guild.ChannelCache.Add(guildChannel);
                }
                
                instance = guildChannel;
            }

            return instance ?? Channel.Create(this, channelJson);
        }
    }
}