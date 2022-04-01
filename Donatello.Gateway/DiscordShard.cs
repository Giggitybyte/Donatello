namespace Donatello.Gateway;

using Donatello.Rest;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// Websocket client for the Discord API gateway.
/// </summary>
public sealed class DiscordShard
{
    private Task _wsReceieveTask, _wsHeartbeatTask;
    private CancellationTokenSource _websocketCts, _heartbeatDelayCts;
    private ChannelWriter<DiscordShard> _identifyChannelWriter;
    private ChannelWriter<DiscordEvent> _eventChannelWriter;
    private ClientWebSocket _websocketClient;
    private DiscordHttpClient _httpClient;
    private ILogger _logger;
    private bool _receivedHeartbeatAck;

    internal DiscordShard(int shardId, DiscordHttpClient httpClient, ChannelWriter<DiscordShard> identifyWriter, ChannelWriter<DiscordEvent> eventWriter, ILogger logger)
    {
        _websocketCts = new CancellationTokenSource();
        _heartbeatDelayCts = new CancellationTokenSource();

        _identifyChannelWriter = identifyWriter;
        _eventChannelWriter = eventWriter;
        _httpClient = httpClient;
        _logger = logger;

        this.Id = shardId;
    }

    /// <summary>Zero-based shard ID number.</summary>
    public int Id { get; private init; }

    /// <summary></summary>
    public int Latency { get; }

    /// <summary>Returns <see langword="true"/> when the websocket connection is active.</summary>
    public bool IsConnected { get => _websocketClient?.State == WebSocketState.Open; }

    /// <summary>Whether or not this shard is sending a regular heartbeat payload to the gateway.</summary>
    public bool IsHeartbeatActive { get => _wsHeartbeatTask.Status == TaskStatus.Running; }

    /// <summary></summary>
    internal string SessionId { get; private set; }

    /// <summary></summary>
    internal int EventSequenceNumber { get; private set; }

    /// <summary>Connects to the Discord gateway.</summary>
    internal async Task ConnectAsync(string gatewayUrl)
    {
        if (this.IsConnected)
            throw new InvalidOperationException("Websocket is already connected.");

        await _websocketClient.ConnectAsync(new Uri($"{gatewayUrl}?v=9&encoding=json"), CancellationToken.None);
        _wsReceieveTask = WebsocketReceiveLoop(_websocketCts.Token);
    }

    /// <summary>Closes the connection with the Discord gateway.</summary>
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

    /// <summary>Sends a payload to the gateway containing an inner data object.</summary>
    internal ValueTask SendPayloadAsync(int opcode, Action<Utf8JsonWriter> dataWriter)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("op", opcode);

        jsonWriter.WriteStartObject("d");
        dataWriter(jsonWriter);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>Sends a payload to the gateway containing a single primitive data value.</summary>
    internal ValueTask SendPayloadAsync(int opcode, JsonValueKind payloadType, object payloadValue = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("op", opcode);

        if (payloadType is JsonValueKind.String && payloadValue is string)
            jsonWriter.WriteString("d", (string)payloadValue);
        else if (payloadType is JsonValueKind.Number && payloadValue is short or int or long)
            jsonWriter.WriteNumber("d", (long)payloadValue);
        else if (payloadType is JsonValueKind.True)
            jsonWriter.WriteBoolean("d", true);
        else if (payloadType is JsonValueKind.False)
            jsonWriter.WriteBoolean("d", false);
        else if (payloadType is JsonValueKind.Null)
            jsonWriter.WriteNull("d");
        else
            throw new JsonException("Invalid payload type.");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary></summary>
    private ValueTask SendHeartbeatAsync()
    {
        if (_wsHeartbeatTask is not null)
            _heartbeatDelayCts.Cancel();

        if (this.EventSequenceNumber is not 0)
            return SendPayloadAsync(1, JsonValueKind.Number, this.EventSequenceNumber);
        else
            return SendPayloadAsync(1, JsonValueKind.Null);
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

            // Read incoming payload from websocket.
            do
            {
                response = await _websocketClient.ReceiveAsync(buffer, CancellationToken.None);
                payloadLength += response.Count;

                if (payloadLength == buffer.Length)
                    ArrayPool<byte>.Shared.Resize(ref buffer, buffer.Length + 4096);

            } while (!response.EndOfMessage);

            // Parse payload as JSON.
            using var payload = JsonDocument.Parse(buffer.AsMemory(0, payloadLength));
            var json = payload.RootElement;
            var opcode = json.GetProperty("op").GetInt32();

            // Handle opcode.
            if (opcode == 0) // Guild, channel, or user event
            {
                this.EventSequenceNumber = json.GetProperty("s").GetInt32();
                var gatewayEvent = new DiscordEvent(this, json.Clone());
                await _eventChannelWriter.WriteAsync(gatewayEvent);
            }
            else if (opcode == 1) // Heartbeat request
                await SendHeartbeatAsync();
            else if (opcode == 7) // Reconnect request
                await ReconnectAsync();
            else if (opcode == 9) // Invalid session
            {
                var resumeable = json.GetProperty("d").GetBoolean();
                if (resumeable is false)
                {
                    this.SessionId = string.Empty;
                    this.EventSequenceNumber = 0;
                }

                await ReconnectAsync();
            }
            else if (opcode == 10) // Identify
            {
                var intervalMs = json.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();
                _wsHeartbeatTask = WebsocketHeartbeatLoop(intervalMs, cancelToken, _heartbeatDelayCts.Token);

                await _identifyChannelWriter.WriteAsync(this);
            }
            else if (opcode == 11) // Heartbeat acknowledgement
                _receivedHeartbeatAck = true;
            else
                throw new NotImplementedException($"Invalid opcode received: {opcode}");


            ArrayPool<byte>.Shared.Return(buffer, true);
        }

        async Task ReconnectAsync()
        {
            await DisconnectAsync();

            var websocketMetadata = await _httpClient.GetGatewayMetadataAsync();
            await ConnectAsync(websocketMetadata.GetProperty("url").GetString());
        }
    }

    /// <summary>
    /// Sends a heartbeat payload to the gateway at a fixed interval.
    /// </summary>
    private async Task WebsocketHeartbeatLoop(int intervalMs, CancellationToken wsToken, CancellationToken delayToken)
    {
        await Task.Delay(intervalMs, delayToken);

        var missedHeartbeats = 0;
        while (!wsToken.IsCancellationRequested)
        {
            await SendHeartbeatAsync();
            await Task.Delay(intervalMs, delayToken);

            if (_receivedHeartbeatAck | delayToken.IsCancellationRequested)
            {
                missedHeartbeats = 0;
                _receivedHeartbeatAck = false;
            }
            else if (++missedHeartbeats >= 5)
            {
                var message = "Discord failed to acknowledge 5 heartbeat payloads; connection is likely dead.";

                _logger.LogCritical(message);
                throw new WebSocketException(message);
            }
            else
                _logger.LogWarning("Discord failed to acknowledge {Number} heartbeat payload(s).", missedHeartbeats);
        }
    }
}





