using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Donatello.Websocket.Client
{
    /// <summary>
    /// Websocket client for the Discord API.
    /// </summary>
    public sealed partial class DiscordClient
    {
        private Task _wsReceieveLoopTask, _wsPayloadProcessingTask, _wsHeartbeatTask;
        private CancellationTokenSource _wsTokenSource;
        private ClientWebSocket _websocketClient;
        private Channel<JsonElement> _payloadChannel;
        private int _lastSequence, _heartbeatIntervalMs;
        private DateTime _lastAcknowledge;
        private string _discordApiToken;

        /// <summary>
        /// Zero-based shard ID number.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Connects to the Discord gateway and begins processing incoming events and commands.
        /// </summary>
        internal async Task ConnectAsync(string gatewayUrl, string apiToken, int id)
        {
            if (_websocketClient?.State == WebSocketState.Open) throw new InvalidOperationException("Websocket is already connected.");
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("Token should not be empty.");

            this.Id = id;
            _discordApiToken = apiToken;
            _wsTokenSource = new CancellationTokenSource();
            _websocketClient = new ClientWebSocket();

            await _websocketClient.ConnectAsync(new Uri(gatewayUrl), _wsTokenSource.Token);
            _wsReceieveLoopTask = await CreateLongRunningTask(WebsocketReceiveLoop).ConfigureAwait(false);
            _wsPayloadProcessingTask = await CreateLongRunningTask(WebsocketPayloadProcessingLoop).ConfigureAwait(false);
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
                _wsPayloadProcessingTask.Wait();
                _wsHeartbeatTask.Wait();

                await _websocketClient.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "", CancellationToken.None);
            }

            _websocketClient.Dispose();
            _wsTokenSource.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        internal ValueTask SendPayload(int opcode, Action<Utf8JsonWriter> payloadWriter)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var jsonWriter = new Utf8JsonWriter(buffer);

            jsonWriter.WriteStartObject();

            jsonWriter.WriteNumber("op", opcode);
            payloadWriter(jsonWriter);

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Writes incoming data from the websocket connection to a <see cref="Channel"/>.
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
                await _payloadChannel.Writer.WriteAsync(payload.RootElement.Clone());

                ArrayPool<byte>.Shared.Return(buffer, true);
            };
        }

        /// <summary>
        /// Reads payload data from a <see cref="Channel"/> and determines how to handle its contents.
        /// </summary>
        private async Task WebsocketPayloadProcessingLoop()
        {
            while (!_wsTokenSource.Token.IsCancellationRequested)
            {
                var payload = await _payloadChannel.Reader.ReadAsync();
                var opcode = payload.GetProperty("op").GetInt32();
                var data = payload.GetProperty("d");

                if (opcode == 0) // Event; see DiscordClient.Dispach
                    await HandleEventDispatch(data).ConfigureAwait(false);

                else if (opcode == 1) // Heartbeat request
                    await SendHeartbeat().ConfigureAwait(false);

                else if (opcode == 7) // Reconnect request
                    throw new NotImplementedException();

                else if (opcode == 9) // Invalid session
                    throw new NotImplementedException();

                else if (opcode == 10) // Hello
                {
                    if (_wsHeartbeatTask is not null) throw new InvalidOperationException("Received 'hello' while heartbeat active.");

                    _heartbeatIntervalMs = data.GetProperty("heartbeat_interval").GetInt32();
                    _wsHeartbeatTask = await CreateLongRunningTask(WebsocketReceiveLoop).ConfigureAwait(false);
                }

                else if (opcode == 11) // Heartbeat ack
                {

                }

                else if (((opcode >= 2) && (opcode <= 6)) | (opcode == 8))
                    throw new NotSupportedException($"Invalid opcode received: {opcode}");

                else
                    throw new NotImplementedException($"Unknown opcode received: {opcode}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async Task WebsocketHeartbeatLoop()
        {
            var wsToken = _wsTokenSource.Token;

            while (!wsToken.IsCancellationRequested)
            {
                await Task.Delay(_heartbeatIntervalMs, wsToken).ConfigureAwait(false);
                await SendHeartbeat().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Internal shortcut method.
        /// </summary>
        private ValueTask SendHeartbeat() 
            => SendPayload(1, (writer) => 
            {
                if (_lastSequence == 0)
                    writer.WriteNull("d");
                else
                    writer.WriteNumber("d", _lastSequence);
            });

        /// <summary>
        /// Internal shortcut method.
        /// </summary>
        private Task<Task> CreateLongRunningTask(Func<Task> function) 
            => Task.Factory.StartNew(function, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
