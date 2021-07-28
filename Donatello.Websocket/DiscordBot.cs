using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Websocket.Commands;
using Donatello.Websocket.Entity;
using Qmmands;
using RestSharp;

namespace Donatello.Websocket
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
        private Channel<JsonElement> _eventChannel;

        internal SortedSet<DiscordShard> _shards;

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

            _restClient = new RestClient("https://discord.com/api");
            _restClient.AddDefaultHeader("Authorization", $"Bot {_apiToken}");

            _shards = new SortedSet<DiscordShard>(new ShardSorter());
            _eventChannel = Channel.CreateUnbounded<JsonElement>();

            _commandService = new CommandService(commandConfig ??= CommandServiceConfiguration.Default);
            _commandService.CommandExecuted += (e) => _commandExecutedEvent.InvokeAsync(e);
            _commandService.CommandExecutionFailed += (e) => _commandExecutionFailedEvent.InvokeAsync(e);
        }

        /// <summary>
        /// Total number of <see cref="DiscordShard"/>s initialized.
        /// </summary>
        public int ShardCount { get => _shards.Count; }

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
        /// Loads an extension for this <see cref="DiscordBot"/> instance.
        /// </summary>
        /// <typeparam name="T">The extension type that will be instanced.</typeparam>
        public void AddExtension<T>() where T : ExtensionBase
        {
            // Create instance; pass this object

            // Store instance in collection
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async ValueTask RemoveExtensionAsync<T>() where T : ExtensionBase
        {
            // Get instance from collection
            // Throw if no
        }

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

            if (batchSize == 1)
            {
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var shard = new DiscordShard(_apiToken, _intents, _eventChannel.Writer);
                    await shard.ConnectAsync(websocketUrl, shardId).ConfigureAwait(false);
                    _shards.Add(shard);
                }
            }
            else
            {
                var shardBatch = new List<DiscordShard>();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    shardBatch.Add(new DiscordShard(_apiToken, _intents, _eventChannel.Writer));

                    if ((shardBatch.Count == batchSize))
                    {
                        var connectionTasks = new List<Task>();
                        foreach (var shard in shardBatch)
                        {
                            var task = shard.ConnectAsync(websocketUrl, shardId);
                            connectionTasks.Add(task);
                        }

                        await Task.WhenAll(connectionTasks).ConfigureAwait(false);

                        foreach (var shard in shardBatch)
                            _shards.Add(shard);

                        shardBatch.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Closes all websocket connections and unloads all extensions.
        /// </summary>
        public async Task StopAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used by the internal <see cref="SortedSet{T}"/> which contains each connected <see cref="DiscordShard"/> shard.
        /// </summary>
        private class ShardSorter : IComparer<DiscordShard>
        {
            public int Compare(DiscordShard shard, DiscordShard other)
            {
                if (shard.Id < other.Id)
                    return -1;
                else if (shard.Id > other.Id)
                    return 1;
                else
                    return 0;
            }
        }
    }
}
