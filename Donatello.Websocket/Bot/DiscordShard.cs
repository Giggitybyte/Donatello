using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Donatello.Websocket.Bot
{
    /// <summary>
    /// Websocket client for the Discord API gateway.
    /// </summary>
    public sealed class DiscordShard
    {
        private Task _wsReceieveTask, _wsHeartbeatTask;
        private CancellationTokenSource _websocketCts, _heartbeatDelayCts;
        private ClientWebSocket _websocketClient;

        private ChannelWriter<GatewayEvent> _eventChannelWriter;

        private int _heartbeatInvervalMs;
        private bool _receivedHeartbeatAck;

        internal DiscordShard(ChannelWriter<GatewayEvent> eventChannelWriter)
        {
            _eventChannelWriter = eventChannelWriter;

            _websocketCts = new CancellationTokenSource();
            _heartbeatDelayCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 
        /// </summary>
        internal string SessionId { get; set; }

        /// <summary>
        /// The most recent event sequence number received from this shard.
        /// </summary>
        internal int LastSequenceNumber { get; set; }

        /// <summary>
        /// Zero-based shard ID number.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public int Latency { get; }

        /// <summary>
        /// Returns <see langword="true"/> when the websocket connection is active.
        /// </summary>
        public bool IsConnected { get => _websocketClient?.State == WebSocketState.Open; }

        /// <summary>
        /// Whether or not this shard is sending a regular heartbeat payload to the gateway.
        /// </summary>
        public bool IsHeartbeatActive { get => _wsHeartbeatTask.Status == TaskStatus.Running; }

        /// <summary>
        /// Connects to the Discord gateway.
        /// </summary>
        internal async Task ConnectAsync(string gatewayUrl)
        {
            if (this.IsConnected)
                throw new InvalidOperationException("Websocket is already connected.");

            _websocketClient ??= new ClientWebSocket();
            _wsReceieveTask = WebsocketReceiveLoop(_websocketCts.Token);

            await _websocketClient.ConnectAsync(new Uri($"{gatewayUrl}?v=9&encoding=json"), CancellationToken.None);
        }

        /// <summary>
        /// Closes the connection with the Discord gateway.
        /// </summary>
        internal async Task DisconnectAsync()
        {
            if (_websocketClient is null)
                throw new InvalidOperationException("Websocket client was not initialized.");

            if (!this.IsConnected)
                throw new InvalidOperationException("Websocket is not connected.");

            _websocketCts.Cancel();
            await _wsReceieveTask;

            _heartbeatDelayCts.Cancel();
            await _wsHeartbeatTask;

            await _websocketClient.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "", CancellationToken.None);
        }

        /// <summary>
        /// Begins regularly sending heartbeat payloads to the gateway.
        /// </summary>
        internal void StartHeartbeat(int intervalMs)
        {
            if (_wsHeartbeatTask?.Status == TaskStatus.Running)
                throw new InvalidOperationException("Heartbeat is already active.");

            _heartbeatInvervalMs = intervalMs;
            _wsHeartbeatTask = WebsocketHeartbeatLoop(_websocketCts.Token, _heartbeatDelayCts.Token);
        }

        /// <summary>
        /// Sends a payload to the gateway containing an inner data object.
        /// </summary>
        internal ValueTask SendPayloadAsync(int opcode, Action<Utf8JsonWriter> objectWriter)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var jsonWriter = new Utf8JsonWriter(buffer);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteNumber("op", opcode);

            jsonWriter.WriteStartObject("d");
            objectWriter(jsonWriter);
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Sends a payload to the gateway containing a single primitive data value.
        /// </summary>
        internal ValueTask SendPayloadAsync(int opcode, JsonValueKind payloadType, object payload)
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
                    jsonWriter.WriteNull("d");
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    jsonWriter.WriteBoolean("d", (bool)payload);
                    break;

                default:
                    throw new JsonException("Invalid payload type.");
            }

            return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ValueTask SendHeartbeatAsync()
        {
            if (_wsHeartbeatTask is not null)
                _heartbeatDelayCts.Cancel();

            if (LastSequenceNumber == 0)
                return SendPayloadAsync(1, JsonValueKind.Null, null);
            else
                return SendPayloadAsync(1, JsonValueKind.Number, LastSequenceNumber);
        }

        /// <summary>
        /// Receives incoming payloads from the gateway connection.
        /// </summary>
        private async Task WebsocketReceiveLoop(CancellationToken cancelToken)
        {
            while (cancelToken!.IsCancellationRequested)
            {
                var payloadLength = 0;
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                WebSocketReceiveResult response;

                do
                {
                    response = await _websocketClient.ReceiveAsync(buffer, CancellationToken.None);
                    payloadLength += response.Count;

                    if (payloadLength == buffer.Length)
                        ArrayPool<byte>.Shared.Resize(ref buffer, buffer.Length + 4096);

                } while (!response.EndOfMessage);

                using var payload = JsonDocument.Parse(buffer.AsMemory(0, payloadLength));
                var opcode = payload.RootElement.GetProperty("op").GetInt32();

                if (opcode == 11) // Heartbeat ack
                    _receivedHeartbeatAck = true;

                else if (opcode == 1) // Heartbeat request
                    await SendHeartbeatAsync();

                else // Not heartbeat related
                {
                    var gatewayEvent = new GatewayEvent(this.Id, payload.RootElement.Clone());
                    await _eventChannelWriter.WriteAsync(gatewayEvent);
                }

                ArrayPool<byte>.Shared.Return(buffer, true);
            };
        }

        /// <summary>
        /// Sends a heartbeat payload to the gateway at a fixed interval.
        /// </summary>
        private async Task WebsocketHeartbeatLoop(CancellationToken wsToken, CancellationToken delayToken)
        {
            await Task.Delay(_heartbeatInvervalMs, delayToken);

            var missedHeartbeats = 0;
            while (!wsToken.IsCancellationRequested)
            {
                await SendHeartbeatAsync();
                await Task.Delay(_heartbeatInvervalMs, delayToken);

                if (_receivedHeartbeatAck)
                {
                    missedHeartbeats = 0;
                    _receivedHeartbeatAck = false;
                }
                else if (++missedHeartbeats >= 3)
                    throw new WebSocketException("Discord missed 3 heartbeat acks.");
            }
        }


    }
}
