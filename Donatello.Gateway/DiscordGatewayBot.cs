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
using Donatello.Cache;
using Donatello.Entity;
using Donatello.Enum;
using Donatello.Extension.Internal;
using Donatello.Gateway.Event;
using Donatello.Gateway.Extension.Internal;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through one or more websocket connections.<br/> 
/// Sends requests to the API through HTTP REST requests and a websocket connection.
/// </remarks>
public sealed class DiscordGatewayBot : DiscordBot
{
    private GuildIntent _intents;
    private DiscordWebsocketShard[] _shards;    
    private List<DiscordSnowflake> _unknownGuilds;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="logFactory"></param>
    public DiscordGatewayBot(string token, GuildIntent intents = GuildIntent.Unprivileged, ILoggerFactory logFactory = null)
        : base(token, logFactory is null ? NullLoggerFactory.Instance.CreateLogger<DiscordGatewayBot>() : logFactory.CreateLogger<DiscordGatewayBot>())
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _shards = Array.Empty<DiscordWebsocketShard>();
        this.Events = Observable.Empty<DiscordEvent>();
        this.DirectChannelCache = new EntityCache<DiscordDirectTextChannel>();
    }

    /// <summary></summary>
    public ReadOnlyCollection<DiscordWebsocketShard> Shards => new(_shards);

    /// <summary>An observable sequence of events received from each shard.</summary>
    public IObservable<DiscordEvent> Events { get; private set; }

    /// <summary>Cached direct message channels.</summary>
    public EntityCache<DiscordDirectTextChannel> DirectChannelCache { get; private init; }

    /// <summary>Whether this instance has an active websocket connection.</summary>
    public override bool IsConnected => _shards.Length > 0 && _shards.All(shard => shard.IsConnected);

    /// <summary>Connects to the Discord gateway.</summary>
    public override async ValueTask StartAsync()
    {
        var websocketMetadata = await this.RestClient.GetGatewayMetadataAsync();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var clusterSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();
        var events = Observable.Empty<DiscordEvent>();

        _shards = new DiscordWebsocketShard[shardCount];

        for (int shardId = 0; shardId < shardCount - 1; shardId++)
        {
            var shard = new DiscordWebsocketShard(shardId, this.Logger);

            shard.Events.Where(eventPayload => eventPayload.GetProperty("op").GetInt32() is 10)
                .Subscribe(eventPayload => this.IdentifyShardAsync(shard).ToObservable());

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
        _shards = Array.Empty<DiscordWebsocketShard>();
    }

    /// <summary>Fetches a user object for </summary>
    public async Task<DiscordUser> GetSelfAsync()
    {
        var json = await this.RestClient.GetSelfAsync();
        var user = new DiscordUser(this, json);
        this.UserCache.Add(user);

        return user;
    }

    /// <summary></summary>
    private async Task IdentifyShardAsync(DiscordWebsocketShard shard)
    {
        if (shard.SessionId is not null)
        {
            await shard.SendPayloadAsync(6, json =>
            {
                json.WriteString("token", this.Token);
                json.WriteString("session_id", shard.SessionId);
                json.WriteNumber("seq", shard.EventIndex);
            });
        }
        else
        {
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
    }

    /// <summary>Projects an observable sequence of raw <see cref="JsonElement"/> payloads to a collection of observable <see cref="DiscordEvent"/> sequences.</summary>
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

                var user = new DiscordUser(this, eventJson.GetProperty("user"));
                this.UserCache.Add(user);

                return new ConnectedEvent();
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_CREATE")
            .Select(eventPayload => DiscordChannel.Create(eventPayload.GetProperty("d"), this))
            .Select(async channel =>
            {
                if (channel is DiscordDirectTextChannel dmChannel)
                    this.DirectChannelCache.Add(dmChannel);
                else if (channel is IGuildChannel guildChannel)
                {
                    var guild = await guildChannel.GetGuildAsync();
                    guild.ChannelCache.Add(guildChannel);
                }

                return new EntityCreatedEvent<DiscordChannel>() { Entity = channel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var updatedChannel = DiscordChannel.Create(eventJson, this);
                DiscordChannel outdatedChannel = null;

                if (updatedChannel is DiscordDirectTextChannel dmChannel)
                    this.DirectChannelCache.Replace(dmChannel);
                else if (updatedChannel is IGuildChannel guildChannel)
                {
                    var guild = await guildChannel.GetGuildAsync();
                    guild.ChannelCache.Add(guildChannel);
                }

                return new EntityUpdatedEvent<DiscordChannel>()
                {
                    UpdatedEntity = updatedChannel,
                    OutdatedEnity = outdatedChannel
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_DELETE")
            .Select(eventPayload => DiscordChannel.Create(eventPayload.GetProperty("d"), this))
            .Select(channel => new EntityDeletedEvent<DiscordChannel>()
            {
                EntityId = channel.Id,
                Instance = channel
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_CREATE")
            .Select(eventPayload => DiscordChannel.Create<DiscordThreadTextChannel>(eventPayload.GetProperty("d"), this))
            .Select(async threadChannel =>
            {
                var guild = await threadChannel.GetGuildAsync();
                guild.ThreadCache.Add(threadChannel.Id, threadChannel);

                return new EntityCreatedEvent<DiscordThreadTextChannel>() { Entity = threadChannel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_UPDATE")
            .Select(eventPayload => DiscordChannel.Create<DiscordThreadTextChannel>(eventPayload.GetProperty("d"), this))
            .Select(async threadChannel =>
            {
                var guild = await threadChannel.GetGuildAsync();
                var outdatedThread = guild.ThreadCache.Replace(threadChannel.Id, threadChannel);

                return new EntityUpdatedEvent<DiscordThreadTextChannel>()
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
                var threadId = eventJson.GetProperty("id").ToSnowflake();
                var guildId = eventJson.GetProperty("guild_id").ToSnowflake();
                var guild = await this.GetGuildAsync(guildId);

                return new EntityDeletedEvent<DiscordThreadTextChannel>()
                {
                    EntityId = threadId,
                    Instance = guild.ThreadCache.Remove(threadId)
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_LIST_SYNC")
           .Select(eventPayload => eventPayload.GetProperty("d"))
           .Select(async eventJson =>
           {
               var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
               var threads = eventJson.GetProperty("threads").EnumerateArray().Select(json => DiscordChannel.Create<DiscordThreadTextChannel>(json, this));
               var members = eventJson.GetProperty("members").EnumerateArray();
               var events = new List<EntityCreatedEvent<DiscordThreadTextChannel>>(threads.Count());

               if (eventJson.TryGetProperty("channel_ids", out JsonElement prop) is false || prop.GetArrayLength() is 0)
                   guild.ThreadCache.Clear();

               IEnumerable<EntityCreatedEvent<DiscordThreadTextChannel>> CreateEvents()
               {
                   foreach (var thread in threads)
                   {
                       foreach (var member in members.Where(json => json.GetProperty("id").GetUInt64() == thread.Id))
                           thread.MemberCache.Add(member.GetProperty("user_id").ToSnowflake(), member);

                       guild.ThreadCache.Add(thread);

                       yield return new EntityCreatedEvent<DiscordThreadTextChannel>() { Entity = thread };
                   }
               }

               return CreateEvents();
           })
           .SelectMany(eventTask => eventTask.ToObservable())
           .SelectMany(events => events.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "THREAD_MEMBERS_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var thread = await guild.GetThreadAsync(eventJson.GetProperty("id").ToSnowflake());
                var newMembers = new List<DiscordThreadMember>();
                var oldMembers = new List<DiscordGuildMember>();

                if (eventJson.TryGetProperty("added_members", out JsonElement addedMembers))
                {
                    foreach (var threadMemberJson in addedMembers.EnumerateArray())
                    {
                        var userId = threadMemberJson.GetProperty("user_id").ToSnowflake();
                        var guildMember = await guild.GetMemberAsync(userId);

                        thread.MemberCache.Add(userId, threadMemberJson);
                        newMembers.Add(new DiscordThreadMember(this, guildMember, threadMemberJson));
                    }
                }

                if (eventJson.TryGetProperty("removed_member_ids", out JsonElement removedUsers))
                {
                    foreach (var userId in removedUsers.EnumerateArray().Select(json => json.ToSnowflake()))
                    {
                        var guildMember = await guild.GetMemberAsync(userId);

                        oldMembers.Add(guildMember);
                        thread.MemberCache.Remove(userId);
                    }
                }

                return new ThreadMembersUpdatedEvent()
                {
                    Thread = thread,
                    New = newMembers.AsReadOnly(),
                    Old = oldMembers.AsReadOnly(),
                };
            })
            .SelectMany(eventTask => eventTask.ToObservable());

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_PINS_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var channel = await guild.GetChannelAsync<DiscordGuildTextChannel>(eventJson.GetProperty("channel_id").ToSnowflake());

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

                var prunedJson = mutableJson.AsElement();
                var guild = new DiscordGuild(this, prunedJson);

                foreach (var member in members.AsArray())
                    guild.MemberCache.Add(member["user"]["id"].AsValue().ToSnowflake(), member.AsElement());

                foreach (var channel in channels.AsArray().Select(node => node.AsObject()))
                    guild.ChannelCache.Add(DiscordChannel.Create<IGuildChannel>(channel, this));

                foreach (var thread in threads.AsArray().Select(node => node.AsObject()))
                    guild.ThreadCache.Add(DiscordChannel.Create<DiscordThreadTextChannel>(thread, this));

                foreach (var state in voiceStates.AsArray().Select(node => node.AsObject()))
                    guild.VoiceStateCache.Add(new DiscordVoiceState(this, state.AsElement(), guild.Id));

                // TODO : user presences

                // TODO : stages

                // TODO : events

                this.GuildCache.Add(guild);
                return new EntityCreatedEvent<DiscordGuild>() { Entity = guild };
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_UPDATE")
            .Select(eventPayload =>
            {
                var updatedGuild = new DiscordGuild(this, eventPayload.GetProperty("d"));
                var outdatedGuild = this.GuildCache.Replace(updatedGuild);

                return new EntityUpdatedEvent<DiscordGuild>()
                {
                    UpdatedEntity = updatedGuild,
                    OutdatedEnity = outdatedGuild
                };
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_DELETE")
            .Select(eventPayload => eventPayload.GetProperty("d").ToSnowflake())
            .Select(guildId => new EntityDeletedEvent<DiscordGuild>()
            {
                EntityId = guildId,
                Instance = this.GuildCache.Remove(guildId)
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_BAN_ADD")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var guild = await this.GetGuildAsync(eventJson.GetProperty("guild_id").ToSnowflake());
                var user = new DiscordUser(this, eventJson.GetProperty("user"));

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
                var user = new DiscordUser(this, eventJson.GetProperty("user"));

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
