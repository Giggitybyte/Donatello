using System;
using System.Buffers;
using System.Net.WebSockets;
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
        internal const int API_VERSION = 9;

        private Task _wsReceieveLoopTask, _wsPayloadProcessingTask, _wsHeartbeatTask;
        private CancellationTokenSource _wsTokenSource;
        private ClientWebSocket _websocketClient;

        private Channel<JsonElement> _payloadChannel;

        private int _heartbeatIntervalMs;
        private int _lastEventSequence;
        private DateTime _lastHeartbeatAck;

        private string _gatewaySessionId;
        private string _discordApiToken;


        /// <summary>
        /// Zero-based shard ID number.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Returns <see langword="true"/> when the websocket connection is active.
        /// </summary>
        public bool IsConnected { get => _websocketClient?.State == WebSocketState.Open; }

        /// <summary>
        /// Connects to the Discord gateway and begins processing incoming payloads and commands.
        /// </summary>
        internal async Task ConnectAsync(string gatewayUrl, string apiToken, int id)
        {
            if (this.IsConnected) throw new InvalidOperationException("Websocket is already connected.");
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("Token should not be empty.");

            this.Id = id;
            _discordApiToken = apiToken;
            _wsTokenSource = new CancellationTokenSource();
            _websocketClient = new ClientWebSocket();

            await _websocketClient.ConnectAsync(new Uri($"{gatewayUrl}?v={API_VERSION}&encoding=json"), CancellationToken.None);
            _wsReceieveLoopTask = await CreateLongRunningTask(WebsocketReceiveLoop).ConfigureAwait(false);
            _wsPayloadProcessingTask = await CreateLongRunningTask(WebsocketPayloadProcessingLoop).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes the open connection with Discord.
        /// </summary>
        internal async Task DisconnectAsync()
        {
            if (_websocketClient is null) throw new InvalidOperationException("Websocket client was not initialized.");
            if (!this.IsConnected) throw new InvalidOperationException("Websocket is already disconnected.");

            _wsTokenSource.Cancel();

            var wsTasks = new[] { _wsReceieveLoopTask, _wsPayloadProcessingTask, _wsHeartbeatTask };
            await Task.WhenAll(wsTasks).ConfigureAwait(false);

            await _websocketClient.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "", CancellationToken.None);

            _websocketClient.Dispose();
            _wsTokenSource.Dispose();
        }

        /// <summary>
        /// Sends a payload to the gateway containing an inner data object.
        /// </summary>
        internal ValueTask SendPayload(int opcode, Action<Utf8JsonWriter> objectWriter)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var jsonWriter = new Utf8JsonWriter(buffer);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteNumber("op", opcode);

            jsonWriter.WritePropertyName("d");

            jsonWriter.WriteStartObject();
            objectWriter(jsonWriter);
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Sends a payload to the gateway containing a single primitive data value.
        /// </summary>
        internal ValueTask SendPayload(int opcode, JsonValueKind payloadType, object payload)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var jsonWriter = new Utf8JsonWriter(buffer);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteNumber("op", opcode);

            switch (payloadType)
            {
                case JsonValueKind.String:
                    jsonWriter.WriteString("d", (string)payload);
                    break;

                case JsonValueKind.Number:
                    jsonWriter.WriteNumber("d", (int)payload);
                    break;

                case JsonValueKind.Null:
                    jsonWriter.WriteNull("d")
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    jsonWriter.WriteBoolean("d", (bool)payload)
                    break;

                default:
                    throw new JsonException("Invalid payload type (wrong overload?)");
            }

            return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
        }


        /// <summary>
        /// Writes incoming data from the websocket connection to the payload <see cref="Channel"/>.
        /// </summary>
        private async Task WebsocketReceiveLoop()
        {
            var wsToken = _wsTokenSource.Token;
            while (!wsToken.IsCancellationRequested)
            {
                var payloadLength = 0;
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                var tempBuffer = ArrayPool<byte>.Shared.Rent(512);
                WebSocketReceiveResult response;                

                do
                {
                    response = await _websocketClient.ReceiveAsync(tempBuffer, CancellationToken.None);

                    Buffer.BlockCopy(tempBuffer, 0, buffer, 0, tempBuffer.Length);
                    Array.Clear(tempBuffer, 0, tempBuffer.Length);

                    payloadLength += response.Count;
                } while (!response.EndOfMessage);

                ArrayPool<byte>.Shared.Return(tempBuffer);

                using var payload = JsonDocument.Parse(buffer.AsMemory(0, payloadLength));
                await _payloadChannel.Writer.WriteAsync(payload.RootElement.Clone()).ConfigureAwait(false);

                ArrayPool<byte>.Shared.Return(buffer, true);
            };
        }

        /// <summary>
        /// Reads payload data from the payload <see cref="Channel"/> and determines how to handle its contents.
        /// </summary>
        private async Task WebsocketPayloadProcessingLoop()
        {
            while (!_wsTokenSource.Token.IsCancellationRequested)
            {
                var payload = await _payloadChannel.Reader.ReadAsync();
                var opcode = payload.GetProperty("op").GetInt32();

                if (opcode == 0) // Event; see DiscordClient.Dispach
                    await HandleEventDispatch(payload.GetProperty("d")).ConfigureAwait(false);

                else if (opcode == 1) // Heartbeat request
                    await SendHeartbeat().ConfigureAwait(false);

                else if (opcode == 7) // Reconnect request
                    throw new NotImplementedException();

                else if (opcode == 9) // Invalid session
                    throw new NotImplementedException();

                else if (opcode == 10) // Hello
                {
                    if (_wsHeartbeatTask is not null) throw new InvalidOperationException("Received 'hello' while heartbeat active.");

                    _heartbeatIntervalMs = payload.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();
                    _wsHeartbeatTask = await CreateLongRunningTask(WebsocketReceiveLoop).ConfigureAwait(false);


                }

                else if (opcode == 11) // Heartbeat ack
                    _lastHeartbeatAck = DateTime.UtcNow;

                else if (((opcode >= 2) && (opcode <= 6)) | (opcode == 8)) // Client opcodes
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
            int missedHeartbeats = 0;
            var wsToken = _wsTokenSource.Token;

            while (!wsToken.IsCancellationRequested)
            {
                await Task.Delay(_heartbeatIntervalMs, wsToken).ConfigureAwait(false);
                await SendHeartbeat().ConfigureAwait(false);

                // TODO: zombie connection check.
            }
        }

        private async Task SendIdentify()
        {

        }

        /// <summary>
        /// Internal shortcut method.
        /// </summary>
        private ValueTask SendHeartbeat()
            => SendPayload(1, (writer) =>
            {
                if (_lastEventSequence == 0)
                    writer.WritePropertyName()
                else
                    writer.WriteNumber("d", _lastEventSequence);
            });

        /// <summary>
        /// Internal shortcut method.
        /// </summary>
        private Task<Task> CreateLongRunningTask(Func<Task> function)
            => Task.Factory.StartNew(function, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
