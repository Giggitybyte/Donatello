namespace Donatello.Gateway;

using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>Websocket client for the Discord API.</summary>
public sealed class WebsocketShard
{
    private ClientWebSocket _websocketClient;
    private Subject<JsonElement> _events;
    private CancellationTokenSource _websocketCts, _heartbeatCts;
    private Task _wsReceiveTask, _wsHeartbeatTask;
    private ILogger _logger;
    private string _sessionResumeUrl;

    internal WebsocketShard(int id, ILogger logger)
    {
        this.Id = id;
        _logger = logger;
        _events = new Subject<JsonElement>();
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
    public IObservable<JsonElement> Payloads => _events.AsObservable();

    /// <summary>Sends a payload to the gateway containing an inner data object.</summary>
    public ValueTask SendPayloadAsync(int opCode, Action<Utf8JsonWriter> jsonDelegate)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("op", opCode);

        jsonWriter.WriteStartObject("d");
        jsonDelegate(jsonWriter);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var jsonString = JsonSerializer.Serialize(buffer.WrittenMemory, new JsonSerializerOptions() { WriteIndented = true });
            _logger.LogTrace("Sending OP {OpCode} payload ({Size} bytes):\n{Json}", opCode, buffer.WrittenCount, jsonString);
        }
        else
            _logger.LogDebug("Sending OP {OpCode} JSON payload ({Size} bytes)", opCode, buffer.WrittenCount);

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
        else if (payloadType is JsonValueKind.Number && payloadValue is short or int or long)
            jsonWriter.WriteNumber("d", Convert.ToInt64(payloadValue));
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

        _logger.LogDebug("Sending OP {OpCode} payload with value '{Value}'", opcode, payloadValue is not null ? payloadValue.ToString() : "null");

        return _websocketClient.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>Connects to the Discord gateway.</summary>
    internal async Task ConnectAsync()
    {
        if (this.IsConnected)
            throw new InvalidOperationException("Websocket is already connected.");

        _websocketClient = new ClientWebSocket();
        _websocketCts = new CancellationTokenSource();
        string gatewayUrl = (_sessionResumeUrl is not null && this.SessionId is not null) ? _sessionResumeUrl : "wss://gateway.discord.gg";

        await _websocketClient.ConnectAsync(new Uri($"{gatewayUrl}?v=10&encoding=json"), CancellationToken.None);

        _wsReceiveTask = this.ListenAsync(_websocketCts.Token)
            .ContinueWith(receiveTask =>
            {
                if (receiveTask.Status is TaskStatus.Faulted)
                {
                    _logger.LogCritical(receiveTask.Exception, "Websocket receive task threw an exception.");
                    _events.OnError(receiveTask.Exception!);
                }    
            });
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
        await _wsReceiveTask;

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

    internal IObservable<Unit> Reconnect()
    {
        return Observable.FromAsync(ct => this.DisconnectAsync(invalidateSession: false))
            .Concat(Observable.FromAsync(ct => this.ConnectAsync()))
            .LastAsync();
    }

    /// <summary>Receives incoming payloads from the gateway connection.</summary>
    private async Task ListenAsync(CancellationToken wsCancelToken)
    {
        var heartbeatAckChannel = Channel.CreateBounded<DateTime>(1);
        byte[] payloadBuffer;
        int payloadByteCount;
        WebSocketReceiveResult response;        

        while (!wsCancelToken.IsCancellationRequested)
        {
            payloadBuffer = ArrayPool<byte>.Shared.Rent(4096);
            payloadByteCount = 0;

            do
            {
                var bufferSegment = new ArraySegment<byte>(payloadBuffer, payloadByteCount, payloadBuffer.Length - payloadByteCount);
                response = await _websocketClient.ReceiveAsync(bufferSegment, CancellationToken.None);
                payloadByteCount += response.Count;

                if (payloadByteCount == payloadBuffer.Length) ResizeBuffer(ref payloadBuffer);
            } while (!response.EndOfMessage);

            var payloadJson = await ParsePayloadAsync();
            _events.OnNext(payloadJson);

            ArrayPool<byte>.Shared.Return(payloadBuffer, true);
        }


        void ResizeBuffer(ref byte[] buffer)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length + 4096);

            Array.Copy(buffer, 0, newBuffer, 0, payloadByteCount);
            ArrayPool<byte>.Shared.Return(buffer, true);

            buffer = newBuffer;
        }

        async ValueTask<JsonElement> ParsePayloadAsync()
        {
            var eventPayload = JsonDocument.Parse(payloadBuffer.AsMemory(0, payloadByteCount));
            var eventJson = eventPayload.RootElement.Clone();
            var opCode = eventJson.GetProperty("op").GetInt32();

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var jsonString = JsonSerializer.Serialize(eventJson, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogTrace("Received OP {OpCode} ({Size} bytes)\n{Json}", opCode, payloadByteCount, jsonString);
            }
            else
                _logger.LogDebug("Received OP {OpCode} ({Size} bytes)", opCode, payloadByteCount);

            if (opCode is 0)
            {
                var eventName = eventJson.GetProperty("t").GetString();
                _logger.LogInformation("Received event {Name}", eventName);

                if (eventName is "READY")
                {
                    this.SessionId = eventJson.GetProperty("d").GetProperty("session_id").GetString();
                    _sessionResumeUrl = eventJson.GetProperty("d").GetProperty("resume_gateway_url").GetString();
                }

                this.EventIndex = eventJson.GetProperty("s").GetInt32();
            }
            else if (opCode is 1)
                _heartbeatCts.Cancel();
            else if (opCode is 7)
                await ReconnectAsync();
            else if (opCode is 9)
            {
                if (eventJson.GetProperty("d").GetBoolean() is false)
                {
                    this.SessionId = string.Empty;
                    this.EventIndex = 0;
                }

                await ReconnectAsync();
            }
            else if (opCode is 10)
            {
                var intervalMs = eventJson.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();
                _wsHeartbeatTask = HeartbeatAsync(intervalMs)
                    .ContinueWith(heartbeatTask =>
                    {
                        if (heartbeatTask.IsFaulted)
                        {
                            _logger.LogError(heartbeatTask.Exception!, "Websocket heartbeat task threw an exception.");
                            _events.OnError(heartbeatTask.Exception!);
                        }
                    });
            }
            else if (opCode is 11)
                await heartbeatAckChannel.Writer.WriteAsync(DateTime.Now);

            return eventJson;
        }

        async Task HeartbeatAsync(int intervalMs)
        {
            ushort missedHeartbeats = 0;

            while (!wsCancelToken.IsCancellationRequested)
            {
                _heartbeatCts = new CancellationTokenSource();
                await Task.Delay(intervalMs, _heartbeatCts.Token)
                    .ContinueWith(delayTask => _logger.LogDebug("Sending heartbeat to gateway."));

                var heartbeatTask = this.EventIndex is not 0
                    ? this.SendPayloadAsync(1, JsonValueKind.Number, this.EventIndex)
                    : this.SendPayloadAsync(1, JsonValueKind.Null);

                await heartbeatTask;
                var lastHeartbeatDate = DateTime.Now;

                var acknowledgementCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await heartbeatAckChannel.Reader.WaitToReadAsync(acknowledgementCts.Token);

                if (heartbeatAckChannel.Reader.Count is not 0)
                {
                    var receiveDate = await heartbeatAckChannel.Reader.ReadAsync();
                    this.Latency = receiveDate - lastHeartbeatDate;
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





