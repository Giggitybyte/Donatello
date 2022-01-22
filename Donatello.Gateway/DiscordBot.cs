namespace Donatello.Gateway;

using System;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Gateway.Command;
using Donatello.Gateway.Entity.Enumeration;
using Donatello.Rest;
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
    private Task _eventProcessingTask;
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
        _httpClient = new DiscordHttpClient(_apiToken);
        _identifyChannel = Channel.CreateUnbounded<DiscordShard>();
        _eventChannel = Channel.CreateUnbounded<DiscordEvent>();

        this.Logger = logger ?? NullLogger.Instance;

        var commandConfig = CommandServiceConfiguration.Default;
        // commandConfig.CooldownBucketKeyGenerator ??= ...;

        _commandService = new CommandService(commandConfig);
        _commandService.CommandExecuted += (s, e) => _commandExecutedEvent.InvokeAsync(this, e);
        _commandService.CommandExecutionFailed += (s, e) => _commandExecutionFailedEvent.InvokeAsync(this, e);

        _shards = Array.Empty<DiscordShard>();
    }

    /// <summary></summary>
    internal ILogger Logger { get; private set; }

    /// <summary></summary>
    internal ReadOnlyList<DiscordShard> Shards { get => new(_shards); }

    /// <summary>Searches the provided assembly for classes which inherit from <see cref="DiscordCommandModule"/> and registers each of their commands.</summary>
    public void LoadCommandModules(Assembly assembly)
        => _commandService.AddModules(assembly);

    /// <summary>Registers all commands found in the provided command module type.</summary>
    public void LoadCommandModule<T>() where T : DiscordCommandModule
        => _commandService.AddModule(typeof(T));

    /// <summary></summary>
    public void LoadCommandModule(Action<ModuleBuilder> moduleBuilder) 
        => _commandService.AddModule(moduleBuilder);

    /// <summary>Adds an addon to this instance.</summary>
    public void LoadAddon<T>() // where T : DonatelloAddon
        => throw new NotImplementedException();

    /// <summary>Removes an addon from this instance.</summary>
    public async ValueTask UnloadAddonAsync<T>() // where T : DonatelloAddon
        => throw new NotImplementedException();

    /// <summary>Connects to the Discord gateway.</summary>
    public async Task StartAsync()
    {
        var payload = await _httpClient.GetGatewayMetadataAsync();

        var websocketUrl = payload.GetProperty("url").GetString();
        var shardCount = payload.GetProperty("shards").GetInt32();
        var batchSize = payload.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new DiscordShard[shardCount];
        _eventProcessingTask = ProcessEventsAsync(_eventChannel.Reader);

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new DiscordShard(shardId, _identifyChannel.Writer, _eventChannel.Writer, this.Logger);
            await shard.ConnectAsync();

            _shards[shardId] = shard;
        }
    }

    /// <summary>Closes all websocket connections and unloads all extensions.</summary>
    public async Task StopAsync()
    {
        if (_shards.Length is 0 | _eventProcessingTask is null)
            throw new InvalidOperationException("This instance is not currently connected to Discord.");

        var disconnectTasks = new Task[_shards.Length];

        foreach (var shard in _shards)
            disconnectTasks[shard.Id] = shard.DisconnectAsync();

        await Task.WhenAll(disconnectTasks);

        _eventChannel.Writer.TryComplete();
        await _eventChannel.Reader.Completion;

        Array.Clear(_shards, 0, _shards.Length);
    }

    /// <summary>Handles</summary>
    private async Task ProcessIdentifyAsync(ChannelReader<DiscordShard> identifyReader)
    {
        // I don't like this.
        await foreach (var shard in identifyReader.ReadAllAsync())
        {
           
        }
    }

    /// <summary>Receives gateway event payloads from each connected <see cref="DiscordShard"/>.</summary>
    private async Task ProcessEventsAsync(ChannelReader<DiscordEvent> eventReader)
    {
        await foreach (var gatewayEvent in eventReader.ReadAllAsync())
        {
            var shard = gatewayEvent.Shard;
            var eventName = gatewayEvent.Payload.GetProperty("t").GetString();
            var eventData = gatewayEvent.Payload.GetProperty("d");


        }
    }
}
