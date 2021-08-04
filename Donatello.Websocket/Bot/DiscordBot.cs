using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Websocket.Command;
using Donatello.Websocket.Entity;
using Qmmands;
using RestSharp;

namespace Donatello.Websocket.Bot
{
    /// <summary>
    /// High-level library and bot framework for the Discord API.<br/>
    /// Receives events from the API through a websocket connection and sends requests to the API through REST requests.
    /// </summary>
    public sealed partial class DiscordBot
    {
        private string _apiToken;
        private DiscordIntent _intents;

        private RestClient _restClient;
        private CommandService _commandService;
        private Channel<GatewayEvent> _eventChannel;

        private Task _eventProcessingTask;
        private CancellationTokenSource _shardCts;

        private int _recommendedShardCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiToken"></param>
        /// <param name="discordConfig"></param>
        /// <param name="commandConfig"></param>
        public DiscordBot(string apiToken, DiscordIntent intents, CommandServiceConfiguration commandConfig = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                throw new ArgumentException("Token cannot be empty.");

            _apiToken = apiToken;
            _intents = intents;
            _eventChannel = Channel.CreateUnbounded<GatewayEvent>();

            _restClient = new RestClient("https://discord.com/api");
            _restClient.AddDefaultHeader("Authorization", $"Bot {_apiToken}");

            commandConfig ??= CommandServiceConfiguration.Default;
            // commandConfig.CooldownBucketKeyGenerator = ...;

            _commandService = new CommandService(commandConfig);
            _commandService.CommandExecuted += (e) => _commandExecutedEvent.InvokeAsync(e);
            _commandService.CommandExecutionFailed += (e) => _commandExecutionFailedEvent.InvokeAsync(e);

            Shards = new SortedList<int, DiscordShard>();
        }

        /// <summary>
        /// Active websocket clients.
        /// </summary>
        internal SortedList<int, DiscordShard> Shards { get; private set; }

        /// <summary>
        /// Searches the provided assembly for classes which inherit from <see cref="DiscordCommandModule"/> and registers each of their commands.
        /// </summary>
        /// <param name="assembly"></param>
        public void LoadCommandModules(Assembly assembly)
            => _commandService.AddModules(assembly);

        /// <summary>
        /// Registers all commands found in the provided command module type.
        /// </summary>
        /// <typeparam name="T">Command module type.</typeparam>
        public void LoadCommandModule<T>() where T : DiscordCommandModule
            => _commandService.AddModule(typeof(T));

        /// <summary>
        /// Loads an extension for this <see cref="DiscordBot"/> instance.<para/>
        /// </summary>
        /// <typeparam name="T">The extension type that will be instanced.</typeparam>
        public void AddExtension<T>() where T : BaseExtension
            => throw new NotImplementedException();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async ValueTask RemoveExtensionAsync<T>() where T : BaseExtension
            => throw new NotImplementedException();

        /// <summary>
        /// Connects to the Discord gateway.
        /// </summary>
        public async Task StartAsync()
        {
            var gatewayInfoRequest = new RestRequest("gateway/bot", Method.GET);
            var gatewayInfoResponse = await _restClient.ExecuteAsync(gatewayInfoRequest).ConfigureAwait(false);

            using var payload = JsonDocument.Parse(gatewayInfoResponse.Content);

            var websocketUrl = payload.RootElement.GetProperty("url").GetString();
            var shardCount = payload.RootElement.GetProperty("shards").GetInt32();
            var batchSize = payload.RootElement.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

            _recommendedShardCount = shardCount;
            _shardCts = new CancellationTokenSource();
            _eventProcessingTask = Task.Run(ShardManagementLoop);

            if (batchSize == 1) // Sequential
            {
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var shard = new DiscordShard(_eventChannel.Writer, _shardCts.Token);
                    await shard.ConnectAsync(websocketUrl, shardId).ConfigureAwait(false);
                    Shards.Add(shardId, shard);
                }
            }
            else // Concurrent batches
            {
                var shards = new List<DiscordShard>();
                while (shards.Count < shardCount)
                    shards.Add(new DiscordShard(_eventChannel.Writer, _shardCts.Token));

                var remainingBatches = shardCount / batchSize;
                var leftoverShards = shardCount % batchSize;
                if (leftoverShards != 0) remainingBatches += 1;

                var lastShardId = 0;
                while (remainingBatches > 0)
                {
                    int count = (remainingBatches == 1 ? leftoverShards : batchSize);
                    var shardBatch = shards.GetRange(lastShardId, count);
                    var batchTasks = new List<Task>();

                    for (int shardId = lastShardId; shardId < (lastShardId + batchSize); shardId++)
                    {
                        var shard = shards[shardId];
                        await shard.ConnectAsync(websocketUrl, shardId).ConfigureAwait(false);
                        Shards.Add(shardId, shard);
                    }

                    remainingBatches--;
                }
            }
        }

        /// <summary>
        /// Closes all websocket connections and unloads all extensions.
        /// </summary>
        public async Task StopAsync()
        {
            
        }

        /// <summary>
        /// Receives gateway event payloads from the event <see cref="Channel"/> and determines how to respond based on the payload opcode.
        /// Either a user facing event will be invoked or the state of the shard which received the payload will be altered.
        /// </summary>
        private async Task ShardManagementLoop()
        {
            await foreach (var gatewayEvent in _eventChannel.Reader.ReadAllAsync())
            {
                var opcode = gatewayEvent.Payload.GetProperty("op").GetInt32();

                if (opcode == 0) // Guild, channel, or user event
                    await DispatchEventAsync(gatewayEvent).ConfigureAwait(false);

                else if (opcode == 7) // Reconnect request
                    throw new NotImplementedException();

                else if (opcode == 9) // Invalid session
                    throw new NotImplementedException();

                else if (opcode == 10) // Hello
                {
                    var interval = gatewayEvent.Payload
                        .GetProperty("d")
                        .GetProperty("heartbeat_interval")
                        .GetInt32();

                    gatewayEvent.Shard.StartHeartbeat(interval);

                    // Identify
                    await gatewayEvent.Shard.SendPayloadAsync(2, (writer) =>
                    {
                        writer.WriteString("token", _apiToken);

                        writer.WriteStartObject("properties");
                        writer.WriteString("$os", Environment.OSVersion.ToString());
                        writer.WriteString("$browser", "Donatello");
                        writer.WriteString("$device", "Donatello");
                        writer.WriteEndObject();

                        writer.WriteNumber("large_threshold", 250);

                        writer.WriteStartArray("shard");
                        writer.WriteNumberValue(gatewayEvent.Shard.Id);
                        writer.WriteNumberValue(_recommendedShardCount);
                        writer.WriteEndArray();

                        writer.WriteNumber("intents", (int)_intents);
                    }).ConfigureAwait(false);
                }

                else if (((opcode >= 2) && (opcode <= 6)) | (opcode == 8)) // Client opcodes
                    throw new NotSupportedException($"Invalid opcode received: {opcode}");

                else
                    throw new NotImplementedException($"Unknown opcode received: {opcode}");
            }
        }
    }
}
