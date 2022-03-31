namespace Donatello.Gateway;

using System;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Core;
using Donatello.Core.Entity;
using Donatello.Core.Enumeration;
using Donatello.Core.Rest.Channel;
using Donatello.Core.Rest.Guild;
using Donatello.Core.Rest.User;
using Donatello.Gateway.Command;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Qmmands;
using Qommon.Collections;

/// <summary>High-level bot framework for the Discord API.</summary>
/// <remarks>
/// Receives events from the API through a websocket connection.<br/> 
/// Sends requests to the API through HTTP REST requests and the websocket connection.
/// </remarks>
public sealed partial class DiscordBot : AbstractBot
{
    private string _apiToken;
    private GatewayIntent _intents;
    private CommandService _commandService;
    private Channel<DiscordShard> _identifyChannel;
    private Channel<DiscordEvent> _eventChannel;
    private DiscordShard[] _shards;
    private Task _identifyProcessingTask, _eventDispatchTask;
    private MemoryCache _guildCache, _channelCache, _userCache, _presenceCache;

    /// <param name="apiToken"></param>
    /// <param name="intents"></param>
    /// <param name="logger"></param>
    public DiscordBot(string apiToken, GatewayIntent intents = GatewayIntent.Unprivileged, ILogger logger = null) : base(apiToken, logger)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Token cannot be empty.");

        _apiToken = apiToken;
        _intents = intents;
        _identifyChannel = Channel.CreateUnbounded<DiscordShard>();
        _eventChannel = Channel.CreateUnbounded<DiscordEvent>();

        _guildCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());
        _userCache = new MemoryCache(new MemoryCacheOptions());
        _presenceCache = new MemoryCache(new MemoryCacheOptions());

        this.Logger = logger ?? NullLogger.Instance;
        InitializeEvents();

        var commandConfig = CommandServiceConfiguration.Default;
        // commandConfig.CooldownBucketKeyGenerator ??= ...;
        _commandService = new CommandService(commandConfig);

        _shards = Array.Empty<DiscordShard>();
    }

    /// <summary></summary>
    internal ILogger Logger { get; private set; }

    /// <summary></summary>
    internal ReadOnlyList<DiscordShard> Shards => new(_shards);

    /// <summary>Searches the provided assembly for classes which inherit from <see cref="DiscordCommandModule"/> and registers each of their commands.</summary>
    public void LoadCommandModules(Assembly assembly)
        => _commandService.AddModules(assembly);

    /// <summary>Registers all commands found in the provided command module type.</summary>
    public void LoadCommandModule<T>() where T : DiscordCommandModule
        => _commandService.AddModule(typeof(T));

    /// <summary></summary>
    public void LoadCommandModule(Action<ModuleBuilder> moduleBuilder)
        => _commandService.AddModule(moduleBuilder);

    /// <summary>Registers a plugin with this instance.</summary>
    public ValueTask EnablePluginAsync<T>() // where T : DonatelloGatewayPlugin
        => throw new NotImplementedException();

    /// <summary>Removes a plugin from this instance, releasing any resources if needed.</summary>
    public async ValueTask DisablePluginAsync<T>() // where T : DonatelloGatewayPlugin
        => throw new NotImplementedException();

    /// <summary>Connects to the Discord gateway.</summary>
    public override async ValueTask StartAsync()
    {
        var websocketMetadata = await this.RestClient.GetGatewayMetadataAsync();

        var websocketUrl = websocketMetadata.GetProperty("url").GetString();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var batchSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new DiscordShard[shardCount];
        _identifyProcessingTask = ProcessShardIdentifyAsync(_identifyChannel.Reader);
        _eventDispatchTask = DispatchGatewayEventsAsync(_eventChannel.Reader);

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new DiscordShard(shardId, this.RestClient, _identifyChannel.Writer, _eventChannel.Writer, this.Logger);
            await shard.ConnectAsync(websocketUrl);

            _shards[shardId] = shard;
        }

        // ...
    }

    /// <summary>Closes all websocket connections and disables all plugins.</summary>
    public override async ValueTask StopAsync()
    {
        if (_shards.Length is 0 | _identifyProcessingTask is null | _eventDispatchTask is null)
            throw new InvalidOperationException("This instance is not currently connected to Discord.");

        var disconnectTasks = new Task[_shards.Length];

        foreach (var shard in _shards)
            disconnectTasks[shard.Id] = shard.DisconnectAsync();

        await Task.WhenAll(disconnectTasks);

        _identifyChannel.Writer.TryComplete();
        _eventChannel.Writer.TryComplete();

        await _identifyChannel.Reader.Completion;
        await _eventChannel.Reader.Completion;

        Array.Clear(_shards, 0, _shards.Length);
    }

    /// <summary>Fetches a guild object using an ID.</summary>
    public override async ValueTask<DiscordGuild> GetGuildAsync(ulong guildId)
    {
        if (_guildCache.TryGetValue<DiscordGuild>(guildId, out var cachedGuild))
            return cachedGuild;
        else
        {
            var json = await this.RestClient.GetGuildAsync(guildId);
            var guild = new DiscordGuild(this, json);

            UpdateGuildCache(guild);
            this.Logger.LogTrace("Added entry {Id} to the guild cache", guildId);

            return guild;
        }
    }

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public override async ValueTask<TChannel> GetChannelAsync<TChannel>(ulong channelId)
    {
        if (_channelCache.TryGetValue<DiscordChannel>(channelId, out var cachedChannel))
            return cachedChannel as TChannel;
        else
        {
            var json = await this.RestClient.GetChannelAsync(channelId);
            var channel = json.ToChannelEntity(this);

            this.Logger.LogTrace("Added entry {Id} to the channel cache", channelId);

            return channel as TChannel;
        }
    }

    /// <summary>Fetches a user object using an ID.</summary>
    public override async ValueTask<DiscordUser> GetUserAsync(ulong userId)
    {
        if (_userCache.TryGetValue<DiscordUser>(userId, out var cachedUser))
            return cachedUser;
        else
        {
            var json = await this.RestClient.GetUserAsync(userId);
            var user = new DiscordUser(this, json);

            UpdateUserCache(user);
            this.Logger.LogTrace("Added entry {Id} to the user cache", userId);

            return user;
        }
    }

    /// <summary>Adds or updates an entry in the guild cache.</summary>
    internal void UpdateGuildCache(DiscordGuild guild)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1))
            .RegisterPostEvictionCallback(LogGuildCacheEviction);

        _guildCache.Set(guild.Id, guild, entryConfig);

        void LogGuildCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the guild cache ({Reason})", (ulong)key, reason);
    }

    /// <summary>Adds or updates an entry in the channel cache.</summary>
    internal void UpdateChannelCache(DiscordChannel channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(channel.Id, channel, entryConfig);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the channel cache ({Reason})", (ulong)key, reason);
    }

    /// <summary>Adds or updates an entry in the user cache.</summary>
    internal void UpdateUserCache(DiscordUser user)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(LogUserCacheEviction);

        _userCache.Set(user.Id, user, entryConfig);

        void LogUserCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the user cache ({Reason})", (ulong)key, reason);
    }

    /// <summary></summary>
    private async Task ProcessShardIdentifyAsync(ChannelReader<DiscordShard> identifyReader)
    {
        await foreach (var shard in identifyReader.ReadAllAsync())
        {
            await shard.SendPayloadAsync(2, json =>
            {
                json.WriteString("token", _apiToken);

                json.WriteStartObject("properties");
                json.WriteString("$os", Environment.OSVersion.ToString());
                json.WriteString("$browser", "Donatello/0.0.0");
                json.WriteString("$browser", "Donatello/0.0.0");
                json.WriteEndObject();

                // json.WriteBoolean("compress", true);

                json.WriteNumber("large_threshold", 250);

                json.WriteStartArray("shard");
                json.WriteNumberValue(shard.Id);
                json.WriteNumberValue(_shards.Length);
                json.WriteEndArray();

                json.WriteNumber("intents", (int)_intents);
            });
        }
    }
}
