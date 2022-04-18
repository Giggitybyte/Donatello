namespace Donatello.Gateway;

using System;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello;
using Donatello.Enumeration;
using Donatello.Gateway.Command;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon.Collections;

/// <summary>Implementation for Discord's real-time websocket API.</summary>
/// <remarks>
/// Receives events from the API through a websocket connection.<br/> 
/// Sends requests to the API through HTTP REST requests and the websocket connection.
/// </remarks>
public sealed partial class DiscordGatewayBot : DiscordApiBot // Everything event related: DiscordGatewayBot.Events.cs
{
    private GatewayIntent _intents;
    private Channel<DiscordWebsocketShard> _identifyChannel;
    private Channel<DiscordEvent> _eventChannel;
    private DiscordWebsocketShard[] _shards;
    private Task _identifyProcessingTask, _eventDispatchTask;

    /// <param name="token"></param>
    /// <param name="intents"></param>
    /// <param name="logger"></param>
    public DiscordGatewayBot(string token, GatewayIntent intents = GatewayIntent.Unprivileged, ILogger logger = null) : base(token, logger)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.");

        _intents = intents;
        _identifyChannel = Channel.CreateUnbounded<DiscordWebsocketShard>();
        _eventChannel = Channel.CreateUnbounded<DiscordEvent>();

        InitializeEvents();

        var commandConfig = CommandServiceConfiguration.Default;
        // commandConfig.CooldownBucketKeyGenerator ??= ...;
        _commandService = new CommandService(commandConfig);

        _shards = Array.Empty<DiscordWebsocketShard>();
    }

    /// <summary></summary>
    internal ReadOnlyList<DiscordWebsocketShard> Shards => new(_shards);

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
    public ValueTask DisablePluginAsync<T>() // where T : DonatelloGatewayPlugin
        => throw new NotImplementedException();

    /// <summary>Connects to the Discord gateway.</summary>
    public override async ValueTask StartAsync()
    {
        var websocketMetadata = await this.RestClient.GetGatewayMetadataAsync();

        var websocketUrl = websocketMetadata.GetProperty("url").GetString();
        var shardCount = websocketMetadata.GetProperty("shards").GetInt32();
        var batchSize = websocketMetadata.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

        _shards = new DiscordWebsocketShard[shardCount];
        _identifyProcessingTask = ProcessShardIdentifyAsync(_identifyChannel.Reader);
        _eventDispatchTask = DispatchGatewayEventsAsync(_eventChannel.Reader);

        for (int shardId = 0; shardId < shardCount; shardId++)
        {
            var shard = new DiscordWebsocketShard(shardId, this.RestClient, _identifyChannel.Writer, _eventChannel.Writer, this.Logger);
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

    /// <summary></summary>
    private async Task ProcessShardIdentifyAsync(ChannelReader<DiscordWebsocketShard> identifyReader)
    {
        await foreach (var shard in identifyReader.ReadAllAsync())
        {
            await shard.SendPayloadAsync(2, json =>
            {
                json.WriteString("token", this.Token);

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
