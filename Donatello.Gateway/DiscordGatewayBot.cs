namespace Donatello.Gateway;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello;
using Donatello.Entity;
using Donatello.Enum;
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
    private DiscordSnowflake _id;
    private List<DiscordSnowflake> _unavailableGuilds;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="logger"></param>
    public DiscordGatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILogger logger = null) : base(token, logger)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _shards = Array.Empty<DiscordWebsocketShard>();
        this.Events = Observable.Empty<DiscordEvent>();
        this.ChannelCache = new EntityCache<DiscordDirectTextChannel>();
    }

    /// <summary></summary>
    public EntityCache<DiscordDirectTextChannel> ChannelCache { get; private init; }

    /// <summary></summary>
    public IReadOnlyList<DiscordWebsocketShard> Shards => new ReadOnlyCollection<DiscordWebsocketShard>(_shards);

    /// <summary>An observable sequence of events received from each shard.</summary>
    public IObservable<DiscordEvent> Events { get; private set; }

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
            var shardEvents = this.GetEventSequences(shard)
               .Merge()
               .Do(eventObject =>
               {
                   eventObject.Bot = this;
                   eventObject.Shard = shard;
               });

            events = events.Merge(shardEvents);

            shard.Events.Where(eventJson => eventJson.GetProperty("op").GetInt32() is 10)
                .Subscribe(eventJson => this.IdentifyShardAsync(shard).ToObservable());

            _shards[shardId] = shard;
        }

        await Observable.Generate(0, i => i < shardCount - 1, i => i + clusterSize, i => _shards.Skip(i).Take(clusterSize), i => TimeSpan.FromSeconds(5))
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
        _shards = Array.Empty<DiscordWebsocketShard>();
    }

    /// <summary>Fetches a user object for </summary>
    public ValueTask<DiscordUser> GetSelfAsync()
        => this.GetUserAsync(_id);

    private async Task IdentifyShardAsync(DiscordWebsocketShard shard)
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
    private IEnumerable<IObservable<DiscordEvent>> GetEventSequences(DiscordWebsocketShard shard)
    {
        var discordEvents = shard.Events.Where(eventJson => eventJson.GetProperty("op").GetInt32() is 0);

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "READY")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(eventJson =>
            {
                var guilds = eventJson.GetProperty("guilds").EnumerateArray()
                    .Select(json => json.GetProperty("id").ToSnowflake());

                _unavailableGuilds.AddRange(guilds);

                var user = new DiscordUser(this, eventJson.GetProperty("user"));
                this.UserCache.Add(user);
                _id = user.Id;

                return new ConnectedEvent();
            });

        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_CREATE")
            .Select(eventPayload => DiscordChannel.Create(eventPayload.GetProperty("d"), this))
            .Select(async channel =>
            {
                if (channel is DiscordDirectTextChannel dmChannel)
                    this.ChannelCache.Add(dmChannel);
                else if (channel is IGuildChannel guildChannel)
                {
                    var guild = await guildChannel.GetGuildAsync();
                    guild.ChannelCache.Add(guildChannel);
                }

                return new EntityAvailableEvent<DiscordChannel>() { Entity = channel };
            })
            .SelectMany(eventTask => eventTask.ToObservable());


        yield return discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "CHANNEL_UPDATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Select(async eventJson =>
            {
                var updatedChannel = DiscordChannel.Create(eventJson, this);
                DiscordChannel outdatedChannel = null;

                if (updatedChannel is DiscordDirectTextChannel dmChannel)
                    this.ChannelCache.Replace(dmChannel);
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
            .Select(channel => new EntityUnavailableEvent<DiscordChannel>()
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

                return new EntityAvailableEvent<DiscordThreadTextChannel>() { Entity = threadChannel };
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

                return new EntityUnavailableEvent<DiscordThreadTextChannel>()
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
               var events = new List<EntityAvailableEvent<DiscordThreadTextChannel>>(threads.Count());

               if (eventJson.TryGetProperty("channel_ids", out JsonElement prop) is false || prop.GetArrayLength() is 0)
                   guild.ThreadCache.Clear();

               IEnumerable<EntityAvailableEvent<DiscordThreadTextChannel>> GetEvents()
               {
                   foreach (var thread in threads)
                   {
                       foreach (var member in members.Where(json => json.GetProperty("id").GetUInt64() == thread.Id))
                           thread.MemberCache.Add(member.GetProperty("user_id").ToSnowflake(), member);

                       guild.ThreadCache.Add(thread);

                       yield return new EntityAvailableEvent<DiscordThreadTextChannel>() { Entity = thread };
                   }
               }

               return GetEvents();
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
                    foreach (var threadMember in addedMembers.EnumerateArray())
                    {
                        var userId = threadMember.GetProperty("user_id").ToSnowflake();
                        var guildMember = await guild.GetMemberAsync(userId);

                        thread.MemberCache.Add(userId, threadMember);
                        newMembers.Add(new DiscordThreadMember(this, guildMember, threadMember));
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
                    NewMembers = newMembers.AsReadOnly(),
                    OldMembers = oldMembers.AsReadOnly(),
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

        discordEvents.Where(eventPayload => eventPayload.GetProperty("t").GetString() is "GUILD_CREATE")
            .Select(eventPayload => eventPayload.GetProperty("d"))
            .Where(eventJson => eventJson.TryGetProperty("unavailable", out var prop) is false || prop.GetBoolean() is false)
            .Select(eventJson =>
            {
                // TODO ...
            });
    }
}
