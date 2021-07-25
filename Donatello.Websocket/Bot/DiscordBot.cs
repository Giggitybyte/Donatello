using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Websocket.Bot.Extension;
using Donatello.Websocket.Client;
using Donatello.Websocket.Commands;
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
        internal const int API_VERSION = 9;

        private string _apiToken;
        private RestClient _restClient;
        private CommandService _commandService;
        private DiscordBotConfiguration _discordConfig;
        internal SortedSet<DiscordClient> _shards;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiToken"></param>
        /// <param name="discordConfig"></param>
        /// <param name="commandConfig"></param>
        public DiscordBot(string apiToken, DiscordBotConfiguration discordConfig = null, CommandServiceConfiguration commandConfig = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("Token cannot be empty.");

            _apiToken = apiToken;
            _discordConfig = discordConfig ?? new DiscordBotConfiguration();

            _restClient = new RestClient("https://discord.com/api");
            _restClient.AddDefaultHeader("Authorization", $"Bot {_apiToken}");

            _discordConfig = discordConfig;
            _shards = new SortedSet<DiscordClient>(ShardSorter.Singleton);
            _commandService = new CommandService(commandConfig ??= CommandServiceConfiguration.Default);

            _commandService.CommandExecuted += (e) => this._commandExecutedEvent.InvokeAsync(e);
            _commandService.CommandExecutionFailed += (e) => this._commandExecutionFailedEvent.InvokeAsync(e);
        }

        /// <summary>
        /// Total number of <see cref="DiscordClient"/>s initialized.
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
        public async Task RemoveExtensionAsync<T>() where T : ExtensionBase
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

            var payload = JsonDocument.Parse(gatewayInfoResponse.Content);

            var websocketUrl = payload.RootElement
                .GetProperty("url")
                .GetString();

            var shardCount = payload.RootElement
                .GetProperty("shards")
                .GetInt32();

            var batchSize = payload.RootElement
                .GetProperty("session_start_limit")
                .GetProperty("max_concurrency")
                .GetInt32();

            payload.Dispose();


            if (batchSize == 1)
            {
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var shard = new DiscordClient();
                    await shard.ConnectAsync(websocketUrl, _apiToken, shardId).ConfigureAwait(false);
                    _shards.Add(shard);
                }
            }
            else
            {
                var shardBatch = new List<DiscordClient>();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    shardBatch.Add(new DiscordClient());

                    if (shardBatch.Count == batchSize)
                    {
                        var connectionTasks = new List<Task>();
                        foreach (var shard in shardBatch)
                        {
                            var task = shard.ConnectAsync(websocketUrl, _apiToken, shardId);
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
        /// Used by the internal <see cref="SortedSet{T}"/> which contains each connected <see cref="DiscordClient"/> shard.
        /// </summary>
        private class ShardSorter : IComparer<DiscordClient>
        {
            public static readonly ShardSorter Singleton = new();

            public int Compare(DiscordClient client, DiscordClient other)
            {
                if (client.Id < other.Id)
                    return -1;
                else if (client.Id > other.Id)
                    return 1;
                else
                    return 0;
            }
        }
    }
}
