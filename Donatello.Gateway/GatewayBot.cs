namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Donatello;
using Donatello.Entity;
using Donatello.Enum;
using Donatello.Extension.Internal;
using Donatello.Gateway.Event;
using Donatello.Gateway.Extension.Internal;
using Donatello.Rest.Extension.Endpoint;
using Donatello.Type;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through one or more websocket connections.<br/> 
/// Sends requests to the API through HTTP REST or a websocket connection.
/// </remarks>
public sealed class GatewayBot : Bot
{
    private ILoggerFactory _loggerFactory;
    private GatewayIntent _intents;
    private WebsocketShard[] _shards;
    private List<Snowflake> _unknownGuilds;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="loggerFactory"></param>
    public GatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILoggerFactory loggerFactory = null)
        : base(token, loggerFactory is null ? NullLoggerFactory.Instance.CreateLogger<GatewayBot>() : loggerFactory.CreateLogger<GatewayBot>())
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _shards = Array.Empty<WebsocketShard>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        this.Events = Observable.Empty<DiscordEvent>();
        this.DirectChannelCache = new EntityCache<DirectTextChannel>();
    }

    /// <summary></summary>
    public ReadOnlyCollection<WebsocketShard> Shards => new(_shards);

    /// <summary>An observable sequence of events received from each shard.</summary>
    public IObservable<DiscordEvent> Events { get; private set; }

    /// <summary>Cached direct message channels.</summary>
    public EntityCache<DirectTextChannel> DirectChannelCache { get; private init; }

    /// <summary>Whether this instance has an active websocket connection.</summary>
    public override bool IsConnected => _shards.Length > 0 && _shards.All(shard => shard.IsConnected);

    /// <summary>Connects to the Discord gateway.</summary>
    public override async ValueTask StartAsync()
    {
        var websocketMetadata = await this.RestClient.GetGatewayMetadataAsync();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var clusterSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();
        var events = Observable.Empty<DiscordEvent>();

        _shards = new WebsocketShard[shardCount];

        for (int shardId = 0; shardId < shardCount - 1; shardId++)
        {
            var shard = new WebsocketShard(shardId, _loggerFactory.CreateLogger($"Shard {shardId}"));

            shard.Events.Where(eventPayload => eventPayload.GetProperty("op").GetInt32() is 10)
                .SelectMany(eventPayload =>  this.IdentifyShardAsync(shard).ToObservable())
                .Subscribe();

            events = this.GetEventSequences(shard.Events)
               .Merge()
               .Do(eventObject =>
               {
                   eventObject.Bot = this;
                   eventObject.Shard = shard;
               })
               .Merge(events);

            _shards[shardId] = shard;
        }

        this.Events = events;

        await Observable.Generate(0, i => i < shardCount - 1, i => i + clusterSize, i => _shards.Skip(i).Take(clusterSize), i => TimeSpan.FromSeconds(5))
            .SelectMany(cluster => cluster.ToObservable())
            .SelectMany(shard => shard.ConnectAsync().ToObservable());
    }

    /// <summary>Closes all websocket connections.</summary>
    public override async ValueTask StopAsync()
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

    /// <summary></summary>
    private async Task IdentifyShardAsync(WebsocketShard shard)
    {
        if (shard.SessionId is not null)
            await shard.SendPayloadAsync(6, json =>
            {
                json.WriteString("token", this.Token);
                json.WriteString("session_id", shard.SessionId);
                json.WriteNumber("seq", shard.EventIndex);
            });
        else
            await shard.SendPayloadAsync(2, json =>
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
    }

    /// <summary>Projects an observable sequence of <see cref="JsonElement"/> payloads to a collection of observable <see cref="DiscordEvent"/> sequences.</summary>
    private IEnumerable<IObservable<DiscordEvent>> GetEventSequences(IObservable<JsonElement> eventSequence)
    {
        var discordEvents = eventSequence.Where(eventJson => eventJson.GetProperty("op").GetInt32() is 0);

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "READY")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(eventJson =>
            {
                var guilds = eventJson.GetProperty("guilds").EnumerateArray()
                    .Select(json => json.GetProperty("id").ToSnowflake());

                _unknownGuilds.AddRange(guilds);
                this.UserCache.Add(new User(this, eventJson.GetProperty("user")));

                return new ConnectedEvent();
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_CREATE")
            .Select(eventPayload => Channel.Create(eventPayload.GetProperty("d"), this))
            .Select(async channel =>
            {
                if (channel is DirectTextChannel dmChannel)
                    this.DirectChannelCache.Add(dmChannel);
                else if (channel is IGuildChannel guildChannel)
                {
                    var guild = await guildChannel.GetGuildAsync();
                    guild.ChannelCache.Add(guildChannel);
                }

                return new EntityCreatedEvent<Channel>() { Entity = channel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var updatedChannel = Channel.Create(eventJson, this);
                Channel outdatedChannel = null;

                if (updatedChannel is DirectTextChannel dmChannel)
                    this.DirectChannelCache.Replace(dmChannel);
                else if (updatedChannel is IGuildChannel guildChannel)
                {
                    var guild = await guildChannel.GetGuildAsync();
                    guild.ChannelCache.Add(guildChannel);
                }

                return new EntityUpdatedEvent<Channel>()
                {
                    UpdatedEntity = updatedChannel,
                    OutdatedEnity = outdatedChannel
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_DELETE")
            .Select(eventPayload => Channel.Create(eventPayload.GetProperty("d"), this))
            .Select(channel => new EntityDeletedEvent<Channel>() { EntityId = channel.Id, Instance = channel });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_CREATE")
            .Select(eventPayload => Channel.Create<GuildThreadChannel>(eventPayload.GetProperty("d"), this))
            .Select(async threadChannel =>
            {                
                var parentChannel = await threadChannel.GetParentChannelAsync();
                parentChannel.ThreadCache.Add(threadChannel.Id, threadChannel);

                return new EntityCreatedEvent<GuildThreadChannel>() { Entity = threadChannel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_UPDATE")
            .Select(eventPayload => Channel.Create<GuildThreadChannel>(eventPayload.GetProperty("d"), this))
            .Select(async threadChannel =>
            {
                var guild = await threadChannel.GetGuildAsync();
                var parentChannel = await threadChannel.GetParentChannelAsync();
                var outdatedThread = parentChannel.ThreadCache.Replace(threadChannel.Id, threadChannel);

                return new EntityUpdatedEvent<GuildThreadChannel>()
                {
                    UpdatedEntity = threadChannel,
                    OutdatedEnity = outdatedThread
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_DELETE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                GuildThreadChannel cachedThread = null;
                var threadId = eventJson.GetProperty("id").ToSnowflake();
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                if (this.GuildCache.TryGet(guildId, out Guild cachedGuild))
                {
                    var parentId = eventJson.GetProperty("parent_id").ToSnowflake();
                    if (cachedGuild.ChannelCache.TryGet(parentId, out IGuildChannel cachedChannel))
                        (cachedChannel as GuildThreadChannel).ThreadCache.TryGet(threadId, out cachedThread);
                }

                return new EntityDeletedEvent<GuildThreadChannel>()
                {
                    EntityId = threadId,
                    Instance = cachedThread
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_LIST_SYNC")
           .Select(eventPayload => eventPayload.GetProperty("d"))
           .Select(async eventJson =>
           {
               var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
               var threads = eventJson.GetProperty("threads").EnumerateArray().Select(json => Channel.Create<GuildThreadChannel>(json, this));
               var members = eventJson.GetProperty("members").EnumerateArray();
               var events = new List<EntityCreatedEvent<GuildThreadChannel>>(threads.Count());

               if (eventJson.TryGetProperty("channel_ids", out JsonElement json) && json.GetArrayLength() is not 0)
               {
                   var parentChannelIds = json.EnumerateArray().Select(json => json.ToSnowflake()).ToList();
                   var cachedChannels = guild.ChannelCache.OfType<GuildTextChannel>().Where(channel => parentChannelIds.Contains(channel.Id));

                   foreach (var thread in threads)
                       parentChannelIds.Remove(thread.ParentId);

                   var filteredParentChannels = guild.ChannelCache.OfType<GuildTextChannel>().Where(channel => parentChannelIds.Contains(channel.Id));
                   foreach (var parentChannel in filteredParentChannels)
                       parentChannel.ThreadCache.Clear();
               }
               else
               {
                   foreach (var cachedGuild in this.GuildCache)
                       foreach (var cachedChannel in cachedGuild.ChannelCache.OfType<GuildTextChannel>())
                           cachedChannel.ThreadCache.Clear();
               }

               foreach (var thread in threads)
               {
                   foreach (var member in members.Where(json => json.GetProperty("id").GetUInt64() == thread.Id))
                       thread.MemberCache.Add(member.GetProperty("user_id").ToSnowflake(), member);

                   var parentChannel = await thread.GetParentChannelAsync();
                   parentChannel.ThreadCache.Add(thread);

                   events.Add(new EntityCreatedEvent<GuildThreadChannel>() { Entity = thread });
               }

               return events;
           })
           .SelectMany(eventTask => eventTask.ToObservable())
           .SelectMany(events => events.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_MEMBERS_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var thread = await guild.GetThreadAsync(eventJson.GetProperty("id").ToSnowflake());
                var eventInstance = new ThreadMembersUpdatedEvent() { Thread = thread };

                if (eventJson.TryGetProperty("added_members", out JsonElement addedMembers))
                {
                    var newMembers = new List<ThreadMember>();
                    foreach (var threadMemberJson in addedMembers.EnumerateArray())
                    {
                        var userId = threadMemberJson.GetProperty("user_id").ToSnowflake();
                        var guildMember = await guild.GetMemberAsync(userId);

                        thread.MemberCache.Add(userId, threadMemberJson);
                        newMembers.Add(new ThreadMember(this, guildMember, threadMemberJson));
                    }

                    eventInstance.New = newMembers.AsReadOnly();
                }

                if (eventJson.TryGetProperty("removed_member_ids", out JsonElement removedUsers))
                {
                    var oldMembers = new List<GuildMember>();
                    foreach (var userId in removedUsers.EnumerateArray().Select(json => json.ToSnowflake()))
                    {
                        var guildMember = await guild.GetMemberAsync(userId);

                        oldMembers.Add(guildMember);
                        thread.MemberCache.Remove(userId);
                    }

                    eventInstance.Old = oldMembers.AsReadOnly();
                }

                return eventInstance;
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_PINS_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var channel = await guild.GetChannelAsync<GuildTextChannel>(eventJson.GetProperty("channel_id").ToSnowflake());

                return new ChannelPinsUpdatedEvent() { Channel = channel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_CREATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Where(eventJson => eventJson.TryGetProperty("unavailable", out var prop) is false || prop.GetBoolean() is false)
            .Select(eventJson =>
            {
                var mutableJson = JsonObject.Create(eventJson);

                mutableJson.Remove("large");
                mutableJson.Remove("member_count");
                mutableJson.Remove("joined_at");

                mutableJson.Remove("members", out JsonNode members);
                mutableJson.Remove("channels", out JsonNode channels);
                mutableJson.Remove("threads", out JsonNode threads);
                mutableJson.Remove("voice_states", out JsonNode voiceStates);
                mutableJson.Remove("presences", out JsonNode presenses);
                mutableJson.Remove("stage_instances", out JsonNode stageInstances);
                mutableJson.Remove("guild_scheduled_events", out JsonNode scheduledEvents);

                var guild = new Guild(this, mutableJson.AsElement());

                foreach (var member in members.AsArray())
                    guild.MemberCache.Add(member["user"]["id"].AsValue().ToSnowflake(), member.AsElement());

                foreach (var channel in channels.AsArray().Select(node => node.AsObject()))
                    guild.ChannelCache.Add(Channel.Create<IGuildChannel>(channel, this));

                foreach (var thread in threads.AsArray().Select(node => node.AsObject()))
                    guild.ThreadCache.Add(Channel.Create<GuildThreadChannel>(thread, this));

                foreach (var state in voiceStates.AsArray().Select(node => node.AsObject()))
                    guild.VoiceStateCache.Add(state["user_id"].AsValue().ToSnowflake(), new DiscordVoiceState(this, state.AsElement()));

                // TODO : user presences

                // TODO : stages

                // TODO : events

                this.GuildCache.Add(guild);
                return new EntityCreatedEvent<Guild>() { Entity = guild };
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_UPDATE")
            .Select(eventPayload =>
            {
                var updatedGuild = new Guild(this, eventPayload.GetProperty("d"));
                var outdatedGuild = this.GuildCache.Replace(updatedGuild);

                return new EntityUpdatedEvent<Guild>()
                {
                    UpdatedEntity = updatedGuild,
                    OutdatedEnity = outdatedGuild
                };
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_DELETE")
            .Select(eventPayload => eventPayload.GetProperty("d").ToSnowflake())
            .Select(guildId => new EntityDeletedEvent<Guild>()
            {
                EntityId = guildId,
                Instance = this.GuildCache.Remove(guildId)
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_BAN_ADD")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var user = new User(this, eventJson.GetProperty("user"));

                this.UserCache.Add(user);

                return new GuildBanEvent()
                {
                    Guild = guild,
                    User = user
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_BAN_REMOVE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var user = new User(this, eventJson.GetProperty("user"));

                this.UserCache.Add(user);

                return new GuildUnbanEvent()
                {
                    Guild = guild,
                    User = user
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());
    }


}
