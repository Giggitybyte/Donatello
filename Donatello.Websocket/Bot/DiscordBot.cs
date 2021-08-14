using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Websocket.Command;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon.Collections;
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
        private DiscordShard[] _shards;

        private ILoggerFactory _loggerFactory;

        private Channel<GatewayEvent> _eventChannel;
        private Task _eventProcessingTask;

        private RestClient _restClient;
        private CommandService _commandService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiToken"></param>
        /// <param name="intents"></param>
        /// <param name="loggerFactory"></param>
        public DiscordBot(string apiToken, DiscordIntent intents = DiscordIntent.Unprivileged, ILoggerFactory loggerFactory = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                throw new ArgumentException("Token cannot be empty.");

            _apiToken = apiToken;
            _intents = intents;
            _eventChannel = Channel.CreateUnbounded<GatewayEvent>();

            _loggerFactory = loggerFactory ?? LoggerFactory.Create(builder => 
            {
                builder.AddSimpleConsole();
            });

            _restClient = new RestClient("https://discord.com/api");
            _restClient.AddDefaultHeader("Authorization", $"Bot {_apiToken}");

            var commandConfig = CommandServiceConfiguration.Default;
            // commandConfig.CooldownBucketKeyGenerator ??= ...;

            _commandService = new CommandService(commandConfig);
            _commandService.CommandExecuted += (e) => _commandExecutedEvent.InvokeAsync(e);
            _commandService.CommandExecutionFailed += (e) => _commandExecutionFailedEvent.InvokeAsync(e);
        }

        /// <summary>
        /// 
        /// </summary>
        internal ReadOnlyList<DiscordShard> Shards { get => new(_shards); }

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
        public void AddExtension<T>() // where T : BaseExtension
            => throw new NotImplementedException();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async ValueTask RemoveExtensionAsync<T>() // where T : BaseExtension
            => throw new NotImplementedException();

        /// <summary>
        /// Connects to the Discord gateway.
        /// </summary>
        public async Task StartAsync()
        {
            var payload = await GetGatewayInfoAsync();

            var websocketUrl = payload.GetProperty("url").GetString();
            var shardCount = payload.GetProperty("shards").GetInt32();
            // var batchSize = payload.GetProperty("session_start_limit").GetProperty("max_concurrency").GetInt32();

            _shards = new DiscordShard[shardCount];
            _eventProcessingTask = ProcessIncomingEventsAsync(_eventChannel.Reader);

            for (int shardId = 0; shardId < shardCount; shardId++)
            {
                var shard = new DiscordShard(_eventChannel.Writer) { Id = shardId };
                await shard.ConnectAsync(websocketUrl);

                _shards[shardId] = shard;
            }
        }

        /// <summary>
        /// Closes all websocket connections and unloads all extensions.
        /// </summary>
        public async Task StopAsync()
        {
            if (_eventProcessingTask is null)
                throw new InvalidOperationException("This instance is not currently connected to Discord.");

            var disconnectTasks = new Task[_shards.Length];
            foreach (var shard in _shards)
                disconnectTasks[shard.Id] = shard.DisconnectAsync();

            await Task.WhenAll(disconnectTasks);

            _eventChannel.Writer.TryComplete();
            await _eventChannel.Reader.Completion;

            Array.Clear(_shards, 0, _shards.Length);
        }

        /// <summary>
        /// Fetches up-to-date gateway connection information from the Discord REST API.
        /// </summary>
        private async Task<JsonElement> GetGatewayInfoAsync()
        {
            var gatewayInfoRequest = new RestRequest("gateway/bot", Method.GET);
            var gatewayInfoResponse = await _restClient.ExecuteAsync(gatewayInfoRequest);
            return JsonDocument.Parse(gatewayInfoResponse.Content).RootElement.Clone();
        }

        /// <summary>
        /// Receives gateway event payloads from each connected <see cref="DiscordShard"/> and determines how to respond based on the payload opcode.
        /// </summary>
        private async Task ProcessIncomingEventsAsync(ChannelReader<GatewayEvent> channelReader)
        {
            await foreach (var gatewayEvent in channelReader.ReadAllAsync())
            {
                var opcode = gatewayEvent.Payload.GetProperty("op").GetInt32();

                if (opcode == 0) // Guild, channel, or user event
                    await DispatchEventAsync(gatewayEvent); // DiscordBot.Dispatch

                else if (opcode == 7) // Reconnect request
                    await ReconnectAsync(_shards[gatewayEvent.ShardId]);

                else if (opcode == 9) // Invalid session
                {
                    var shard = _shards[gatewayEvent.ShardId];
                    var resumeable = gatewayEvent.Payload.GetProperty("d").GetBoolean();

                    if (!resumeable)
                    {
                        shard.SessionId = string.Empty;
                        shard.LastSequenceNumber = 0;
                    }

                    await ReconnectAsync(shard);
                }

                else if (opcode == 10) // Hello
                {
                    var shard = _shards[gatewayEvent.ShardId];
                    var interval = gatewayEvent.Payload.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();

                    shard.StartHeartbeat(interval);

                    if (string.IsNullOrEmpty(shard.SessionId))
                    {
                        await shard.SendPayloadAsync(2, (writer) =>
                        {
                            writer.WriteString("token", _apiToken);

                            writer.WriteStartObject("properties");
                            writer.WriteString("$os", Environment.OSVersion.ToString());
                            writer.WriteString("$browser", "Donatello");
                            writer.WriteString("$device", "Donatello");
                            writer.WriteEndObject();

                            writer.WriteNumber("large_threshold", 250);

                            writer.WriteStartArray("shard");
                            writer.WriteNumberValue(shard.Id);
                            writer.WriteNumberValue(_shards.Length);
                            writer.WriteEndArray();

                            writer.WriteNumber("intents", (int)_intents);
                        });
                    }
                    else
                    {
                        await shard.SendPayloadAsync(6, (writer) =>
                        {
                            writer.WriteString("token", _apiToken);
                            writer.WriteString("session_id", shard.SessionId);
                            writer.WriteNumber("seq", shard.LastSequenceNumber);
                        });
                    }
                }

                else
                    throw new NotImplementedException($"Unknown opcode received: {opcode}");
            }

            async Task ReconnectAsync(DiscordShard shard)
            {
                var connectionInfo = await GetGatewayInfoAsync();
                var url = connectionInfo.GetProperty("url").GetString();

                await shard.DisconnectAsync();
                await shard.ConnectAsync(url);
            }
        }
    }
}
