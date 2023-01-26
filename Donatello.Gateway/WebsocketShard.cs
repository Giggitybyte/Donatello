namespace Donatello.Gateway;

using Donatello.Gateway.Extension.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>Websocket client for the Discord API.</summary>
public sealed class WebsocketShard
{
    private ClientWebSocket _websocketClient;
    private Subject<JsonElement> _eventSequence;
    private CancellationTokenSource _websocketCts, _heartbeatCts;
    private Task _wsReceieveTask, _wsHeartbeatTask;
    private ILogger _logger;
    private string _sessionResumeUrl;

    internal WebsocketShard(int id, ILogger logger)
    {
        this.Id = id;
        _logger = logger;
        _eventSequence = new Subject<JsonElement>();
    }

    /// <summary>Last received event sequence number.</summary>
    internal int EventIndex { get; private set; }

    /// <summary>Gateway session ID.</summary>
    public string SessionId { get; private set; }

    /// <summary>Zero-based shard ID number.</summary>
    public int Id { get; private init; }

    /// <summary></summary>
    public TimeSpan Latency { get; private set; }

    /// <summary>Returns <see langword="true"/> when the websocket connection is active.</summary>
    public bool IsConnected => _websocketClient?.State == WebSocketState.Open;

    /// <summary>Whether or not this shard is sending a regular heartbeat payload to the gateway.</summary>
    public bool IsHeartbeatActive => _wsHeartbeatTask?.Status == TaskStatus.Running;

    /// <summary>An observable sequence of raw gateway payloads received by this shard.</summary>
    public IObservable<JsonElement> Events => _eventSequence;

    /// <summary>Sends a payload to the gateway containing an inner data object.</summary>
    public ValueTask SendPayloadAsync(int opcode, Action<Utf8JsonWriter> jsonDelegate)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("op", opcode);

        jsonWriter.WriteStartObject("d");
        jsonDelegate(jsonWriter);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>Sends a payload to the gateway containing a single primitive data value.</summary>
    public ValueTask SendPayloadAsync(int opcode, JsonValueKind payloadType, object payloadValue = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("op", opcode);

        if (payloadType is JsonValueKind.String && payloadValue is string)
            jsonWriter.WriteString("d", payloadValue as string);
        else if (payloadType is JsonValueKind.Number && payloadValue is short or int)
            jsonWriter.WriteNumber("d", (int)payloadValue);
        else if (payloadType is JsonValueKind.True)
            jsonWriter.WriteBoolean("d", true);
        else if (payloadType is JsonValueKind.False)
            jsonWriter.WriteBoolean("d", false);
        else if (payloadType is JsonValueKind.Null)
            jsonWriter.WriteNull("d");
        else
            throw new JsonException("Invalid payload.");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>Connects to the Discord gateway.</summary>
    internal Task ConnectAsync()
    {
        if (this.IsConnected)
            throw new InvalidOperationException("Websocket is already connected.");

        _websocketClient = new ClientWebSocket();
        _websocketCts = new CancellationTokenSource();
        _wsReceieveTask = this.ListenAsync(_websocketCts.Token);

        string gatewayUrl = (_sessionResumeUrl is not null && this.SessionId is not null) ? _sessionResumeUrl : "wss://gateway.discord.gg";
        return _websocketClient.ConnectAsync(new Uri($"{gatewayUrl}?v=10&encoding=json"), CancellationToken.None);
    }

    /// <summary>Closes the connection with the Discord gateway.</summary>
    internal async Task DisconnectAsync(bool invalidateSession = true)
    {
        if (_websocketClient is null)
            throw new InvalidOperationException("Websocket client was not initialized.");

        if (!this.IsConnected)
            throw new InvalidOperationException("Websocket is not connected.");

        _heartbeatCts.Cancel();
        _websocketCts.Cancel();
        await _wsHeartbeatTask;
        await _wsReceieveTask;

        WebSocketCloseStatus closeStatus;

        if (invalidateSession)
        {
            closeStatus = WebSocketCloseStatus.EndpointUnavailable;

            _sessionResumeUrl = null;
            this.SessionId = null;
            this.EventIndex = 0;
        }
        else
            closeStatus = WebSocketCloseStatus.Empty;

        await _websocketClient.CloseAsync(closeStatus, "User requested disconnect.", CancellationToken.None);
        _logger.LogInformation("Disconnected shard {Id}", this.Id);
    }

    /// <summary>Receives incoming payloads from the gateway connection.</summary>
    private async Task ListenAsync(CancellationToken wsCancelToken)
    {
        var heartbeatAckChannel = Channel.CreateBounded<DateTime>(1);

        while (!wsCancelToken.IsCancellationRequested)
        {
            var payloadSize = 0;
            var buffer = ArrayPool<byte>.Shared.Rent(8192);
            WebSocketReceiveResult response;

            do
            {
                response = await _websocketClient.ReceiveAsync(buffer, CancellationToken.None);
                payloadSize += response.Count;

                if (payloadSize == buffer.Length)
                    ArrayPool<byte>.Shared.Resize(ref buffer, buffer.Length + 4096);

            } while (!response.EndOfMessage);

            var eventPayload = JsonDocument.Parse(buffer.AsMemory(0, payloadSize));
            var eventJson = eventPayload.RootElement;
            var opcode = eventJson.GetProperty("op").GetInt32();

            if (opcode is 0)
            {
                var eventName = eventJson.GetProperty("t").GetString();
                _logger.LogDebug("Received {Name} event from Discord.", eventName);

                if (eventName is "READY")
                {
                    this.SessionId = eventJson.GetProperty("d").GetProperty("session_id").GetString();
                    _sessionResumeUrl = eventJson.GetProperty("d").GetProperty("resume_gateway_url").GetString();
                }

                this.EventIndex = eventJson.GetProperty("s").GetInt32();
            }
            else if (opcode is 1)
                _heartbeatCts.Cancel();
            else if (opcode is 7)
                await ReconnectAsync();
            else if (opcode is 9)
            {
                if (eventJson.GetProperty("d").GetBoolean() is false)
                {
                    this.SessionId = string.Empty;
                    this.EventIndex = 0;
                }

                await ReconnectAsync();
            }
            else if (opcode is 10)
            {
                var intervalMs = eventJson.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();
                _wsHeartbeatTask = HeartbeatAsync(intervalMs);
            }
            else if (opcode is 11)
                await heartbeatAckChannel.Writer.WriteAsync(DateTime.Now);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var formattedJson = JsonSerializer.Serialize(eventJson, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogTrace("Received {Size} byte payload:\n{Json}", payloadSize, formattedJson);
            }

            _eventSequence.OnNext(eventJson.Clone());

            eventPayload.Dispose();
            ArrayPool<byte>.Shared.Return(buffer, true);
        }

        async Task HeartbeatAsync(int intervalMs)
        {
            DateTime lastHeartbeartDate = default;
            ushort missedHeartbeats = 0;

            while (!wsCancelToken.IsCancellationRequested)
            {
                if (_heartbeatCts is null || _heartbeatCts.IsCancellationRequested)
                    _heartbeatCts = new CancellationTokenSource();

                var delayTime = Convert.ToInt32(intervalMs - (DateTime.Now - lastHeartbeartDate).TotalMilliseconds);
                await Task.Delay(delayTime, _heartbeatCts.Token);

                var heartbeatTask = this.EventIndex is not 0
                    ? this.SendPayloadAsync(1, JsonValueKind.Number, this.EventIndex)
                    : this.SendPayloadAsync(1, JsonValueKind.Null);

                await heartbeatTask;
                lastHeartbeartDate = DateTime.Now;

                var acknowledgementCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await heartbeatAckChannel.Reader.WaitToReadAsync(acknowledgementCts.Token);

                if (heartbeatAckChannel.Reader.Count is not 0)
                {
                    var receiveDate = await heartbeatAckChannel.Reader.ReadAsync();
                    this.Latency = receiveDate - lastHeartbeartDate;
                    missedHeartbeats = 0;
                }
                else
                    ++missedHeartbeats;

                if (missedHeartbeats > 3)
                {
                    _logger.LogWarning("Discord failed to acknowledge more than 3 heartbeat payloads; reconnecting.");
                    await ReconnectAsync();

                    break;
                }
                else if (missedHeartbeats is not 0)
                    _logger.LogWarning("Discord failed to acknowledge {Number} heartbeat payloads.", missedHeartbeats);
            }
        }

        async Task ReconnectAsync()
        {
            await this.DisconnectAsync(invalidateSession: false);
            await this.ConnectAsync();
        }
    }
}





