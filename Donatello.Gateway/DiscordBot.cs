namespace Donatello.Gateway;

using System;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Gateway.Command;
using Donatello.Gateway.Entity;
using Donatello.Gateway.Enumeration;
using Donatello.Rest;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Qmmands;
using Qommon.Collections;

/// <summary>
/// High-level bot framework for the Discord API.<br/>
/// Receives events from the API through a websocket connection and sends requests to the API through HTTP REST requests.
/// </summary>
public sealed partial class DiscordBot
{
    private string _apiToken;
    private DiscordIntent _intents;
    private DiscordHttpClient _httpClient;
    private CommandService _commandService;
    private Channel<DiscordShard> _identifyChannel;
    private Channel<DiscordEvent> _eventChannel;
    private Task _identifyProcessingTask, _eventDispatchTask;
    private DiscordShard[] _shards;

    /// <param name="apiToken"></param>
    /// <param name="intents"></param>
    /// <param name="logger"></param>
    public DiscordBot(string apiToken, DiscordIntent intents = DiscordIntent.Unprivileged, ILogger logger = null)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Token cannot be empty.");

        _apiToken = apiToken;
        _intents = intents;
        _httpClient = new DiscordHttpClient(apiToken);
        _identifyChannel = Channel.CreateUnbounded<DiscordShard>();
        _eventChannel = Channel.CreateUnbounded<DiscordEvent>();

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
    internal ReadOnlyList<DiscordShard> Shards { get => new(_shards); }

    /// <summary></summary>
    internal EntityCache Cache { get; private init; }

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
    public void EnablePlugin<T>() // where T : DonatelloGatewayPlugin
        => throw new NotImplementedException();

    /// <summary>Removes a plugin from this instance, releasing any resources if needed.</summary>
    public async ValueTask DisablePluginAsync<T>() // where T : DonatelloGatewayPlugin
        => throw new NotImplementedException();

    /// <summary>Connects to the Discord gateway.</summary>
    public async Task StartAsync()
    {
        var websocketMetadata = await _httpClient.GetGatewayMetadataAsync();

        var websocketUrl = websocketMetadata.GetProperty("url").GetString();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var batchSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new DiscordShard[shardCount];
        _identifyProcessingTask = ProcessIdentifyAsync(_identifyChannel.Reader);
        _eventDispatchTask = DispatchGatewayEventsAsync(_eventChannel.Reader);

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new DiscordShard(shardId, _httpClient, _identifyChannel.Writer, _eventChannel.Writer, this.Logger);
            await shard.ConnectAsync(websocketUrl);

            _shards[shardId] = shard;
        }

        // ...
    }

    /// <summary>Closes all websocket connections and unloads all extensions.</summary>
    public async Task StopAsync()
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

    public async ValueTask<DiscordChannel> GetChannelAsync(ulong channelId)
    {

    }

    /// <summary>Handles</summary>
    private async Task ProcessIdentifyAsync(ChannelReader<DiscordShard> identifyReader)
    {
        await foreach (var shard in identifyReader.ReadAllAsync())
        {
            await shard.SendPayloadAsync(2, (json) =>
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

    internal class EntityCache
    {
        internal EntityCache()
        {
           var userOptions = new MemoryCacheOptions()
           {
                
           }
           this.Users = new MemoryCache(userOptions);
        }

        /// <summary></summary>
        internal MemoryCache Users { get; private init; }

        /// <summary></summary>
        internal MemoryCache Presences { get; private init; }

        /// <summary></summary>
        internal MemoryCache Guilds { get; private init; }

        /// <summary></summary>
        internal MemoryCache Channels { get; private init; }

        /// <summary></summary>
        internal MemoryCache Messages { get; private init; }
    }
}
