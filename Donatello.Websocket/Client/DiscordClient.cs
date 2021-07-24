using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Donatello.Websocket.Client
{
    /// <summary>
    /// Websocket client for the Discord API.
    /// </summary>
    public sealed partial class DiscordClient
    {
        private string _discordApiToken;
        private ClientWebSocket _websocketClient;
        private CancellationTokenSource _wsTokenSource;
        private Task _wsReceieveLoopTask, _wsHeartbeatTask;

        /// <summary>
        /// Zero-based shard ID number.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Connects to the Discord gateway and begins processing incoming events and commands.
        /// </summary>
        internal async Task ConnectAsync(string gatewayUrl, string apiToken)
        {
            if (_websocketClient?.State == WebSocketState.Open) throw new InvalidOperationException("Websocket is already connected.");
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("Token should not be empty.");

            _discordApiToken = apiToken;
            _wsTokenSource = new CancellationTokenSource();
            _websocketClient = new ClientWebSocket();

            await _websocketClient.ConnectAsync(new Uri(gatewayUrl), _wsTokenSource.Token);
            _wsReceieveLoopTask = await Task.Factory.StartNew(WebsocketReceiveLoop, _wsTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Closes the open connection with Discord.
        /// </summary>
        internal async Task DisconnectAsync()
        {
            if (_websocketClient is null) throw new InvalidOperationException("Websocket client was not initialized.");

            if (_websocketClient.State == WebSocketState.Open)
            {
                _wsTokenSource.Cancel();
                _wsReceieveLoopTask.Wait();

                await _websocketClient.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "", CancellationToken.None);
            }

            _websocketClient.Dispose();
            _wsTokenSource.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        internal Task SendPayload(string payload)
            => _websocketClient.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, _wsTokenSource.Token);

        /// <summary>
        /// 
        /// </summary>
        private async Task WebsocketReceiveLoop()
        {
            var wsToken = _wsTokenSource.Token;
            while (!wsToken.IsCancellationRequested)
            {
                WebSocketReceiveResult response;
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                var tempBuffer = ArrayPool<byte>.Shared.Rent(512);
                var payloadLength = 0;

                do
                {
                    response = await _websocketClient.ReceiveAsync(tempBuffer, CancellationToken.None);

                    Buffer.BlockCopy(tempBuffer, 0, buffer, 0, tempBuffer.Length);
                    Array.Clear(tempBuffer, 0, tempBuffer.Length);

                    payloadLength += response.Count;
                } while (!response.EndOfMessage);

                ArrayPool<byte>.Shared.Return(tempBuffer);

                using var payload = JsonDocument.Parse(buffer.AsMemory(0, payloadLength));
                var opcode = payload.RootElement.GetProperty("op").GetInt32();
                var data = payload.RootElement.GetProperty("d");

                ArrayPool<byte>.Shared.Return(buffer, true);

                if (opcode == 0) // Event dispatch
                {
                    // See: DiscordClient.Dispatch
                    HandleEventDispatch(data.Clone());
                }
                else if (opcode == 1) // Heartbeat
                {

                }
                else if (opcode == 7) // Reconnect
                {

                }
                else if (opcode == 9) // Invalid session
                {

                }
                else if (opcode == 10) // Hello
                {

                }
                else if (opcode == 11) // Heartbeat ack
                {

                }
                else if (((opcode >= 2) && (opcode <= 6)) | (opcode == 8))
                    throw new NotSupportedException($"Invalid opcode received: {opcode}");
                else
                    throw new NotImplementedException($"Unknown opcode received: {opcode}");
            };
        }
    }
}
