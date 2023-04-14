namespace Donatello.Gateway;

using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Common.Enum;

/// <summary>Websocket client for the Discord API.</summary>
public sealed class WebsocketShard
{
    private ClientWebSocket _websocketClient;
    private Subject<JsonElement> _payloadSubject;
    private TaskPoolScheduler _payloadScheduler;
    private CancellationTokenSource _websocketCts, _heartbeatCts;
    private Task _wsReceiveTask, _wsHeartbeatTask;
    private string _sessionResumeUrl;
    private ILogger _logger;

    internal WebsocketShard(int id, ILogger logger)
    {
        this.Id = id;
        _logger = logger;
        _payloadSubject = new Subject<JsonElement>();
        _payloadScheduler = new TaskPoolScheduler();
    }
    
    /// <summary>Zero-based shard ID number.</summary>
    public int Id { get; }
    
    /// <summary>Gateway session ID.</summary>
    public string SessionId { get; private set; }

    /// <summary>Last received event sequence number.</summary>
    public int EventIndex { get; private set; }

    /// <summary></summary>
    public TimeSpan Latency { get; private set; }

    /// <summary>Returns <see langword="true"/> when the websocket connection is active.</summary>
    public bool IsConnected => _websocketClient?.State == WebSocketState.Open;

    /// <summary>Whether or not this shard is sending a regular heartbeat payload to the gateway.</summary>
    public bool IsHeartbeatActive => _wsHeartbeatTask?.Status == TaskStatus.Running;

    /// <summary>An observable sequence of raw gateway payloads received by this shard.</summary>
    public IObservable<JsonElement> Payloads => _payloadSubject.AsObservable().SubscribeOn(_payloadScheduler).ObserveOn(_payloadScheduler);

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
            var jsonString = JsonSerializer.Serialize(buffer.WrittenMemory);
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

        if (payloadType is JsonValueKind.Null | payloadValue is null)
            jsonWriter.WriteNull("d");
        if (payloadType is JsonValueKind.String && payloadValue is string payloadString)
            jsonWriter.WriteString("d", payloadString);
        else if (payloadType is JsonValueKind.Number && payloadValue is short or int or long)
            jsonWriter.WriteNumber("d", Convert.ToInt64(payloadValue));
        else if (payloadType is JsonValueKind.Number && payloadValue is ushort or uint or ulong)
            jsonWriter.WriteNumber("d", Convert.ToUInt64(payloadValue));
        else if (payloadType is JsonValueKind.Number && payloadValue is decimal payloadDecimal)
            jsonWriter.WriteNumber("d", payloadDecimal);
        else if (payloadType is JsonValueKind.Number && payloadValue is double payloadDouble)
            jsonWriter.WriteNumber("d", payloadDouble);
        else if (payloadType is JsonValueKind.True)
            jsonWriter.WriteBoolean("d", true);
        else if (payloadType is JsonValueKind.False)
            jsonWriter.WriteBoolean("d", false);
        else
            throw new JsonException("Invalid payload.");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        _logger.LogDebug("Sending OP {OpCode} with value '{Value}'", opcode, payloadValue is not null ? payloadValue.ToString() : "null");
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

        _wsReceiveTask = this.ListenAsync(_websocketCts.Token).ContinueWith(receiveTask =>
        {
            if (receiveTask.Status is TaskStatus.Faulted)
            {
                _logger.LogCritical(receiveTask.Exception!, "Websocket receive task threw an exception.");
                _payloadSubject.OnError(receiveTask.Exception!);
            }
        });
    }

    /// <summary></summary>
    internal Task IdentifyAsync(string token, GatewayIntent intents, int totalShards)
    {
        Task requestTask;

        if (this.SessionId is null)
        {
            requestTask = this.SendPayloadAsync(2, json =>
            {
                json.WriteString("token", token);

                json.WriteStartObject("properties");
                json.WriteString("os", Environment.OSVersion.ToString());
                json.WriteString("browser", "Donatello/0.0.0");
                json.WriteString("device", "Donatello/0.0.0");
                json.WriteEndObject();

                json.WriteStartArray("shard");
                json.WriteNumberValue(this.Id);
                json.WriteNumberValue(totalShards);
                json.WriteEndArray();

                json.WriteNumber("intents", (int)intents);
                json.WriteNumber("large_threshold", 250);
                // json.WriteBoolean("compress", true);
            }).AsTask();
        }
        else
        {
            requestTask = this.SendPayloadAsync(6, json =>
            {
                json.WriteString("token", token);
                json.WriteString("session_id", this.SessionId);
                json.WriteNumber("seq", this.EventIndex);
            }).AsTask();
        }

        return requestTask;
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
        => Observable.FromAsync(ct => this.DisconnectAsync(invalidateSession: false))
            .Concat(Observable.FromAsync(ct => this.ConnectAsync()))
            .LastAsync();

    /// <summary>Receives incoming payloads from the gateway connection.</summary>
    private async Task ListenAsync(CancellationToken wsCancelToken)
    {
        byte[] payloadBuffer;
        int payloadByteCount;
        WebSocketReceiveResult response;
        var heartbeatAckChannel = Channel.CreateBounded<DateTime>(1);

        while (!wsCancelToken.IsCancellationRequested)
        {
            payloadBuffer = ArrayPool<byte>.Shared.Rent(4096);
            payloadByteCount = 0;

            do
            {
                if (payloadByteCount == payloadBuffer.Length) ResizeBuffer(ref payloadBuffer);

                var bufferSegment = new ArraySegment<byte>(payloadBuffer, payloadByteCount, payloadBuffer.Length - payloadByteCount);
                response = await _websocketClient.ReceiveAsync(bufferSegment, CancellationToken.None);
                payloadByteCount += response.Count;
            } while (!response.EndOfMessage);

            var payloadJson = await ParsePayloadAsync();
            _payloadSubject.OnNext(payloadJson);

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
                var jsonString = JsonSerializer.Serialize(eventJson);
                _logger.LogTrace("Received OP {OpCode} ({Size} bytes): {Json}", opCode, payloadByteCount, jsonString);
            }
            else if (opCode is not 0)
                _logger.LogDebug("Received OP {OpCode} ({Size} bytes)", opCode, payloadByteCount);

            if (opCode is 0)
            {
                var eventName = eventJson.GetProperty("t").GetString();

                if (eventName is "READY")
                {
                    this.SessionId = eventJson.GetProperty("d").GetProperty("session_id").GetString();
                    _sessionResumeUrl = eventJson.GetProperty("d").GetProperty("resume_gateway_url").GetString();
                }

                if (_logger.IsEnabled(LogLevel.Trace) is false)
                    _logger.LogDebug("Received '{Name}' event", eventName);

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
                _wsHeartbeatTask = HeartbeatAsync(intervalMs).ContinueWith(heartbeatTask =>
                {
                    if (heartbeatTask.IsFaulted)
                    {
                        _logger.LogError(heartbeatTask.Exception!, "Websocket heartbeat task threw an exception.");
                        _payloadSubject.OnError(heartbeatTask.Exception!);
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
                await Task.Delay(intervalMs, _heartbeatCts.Token).ContinueWith(delayTask => _logger.LogDebug("Sending heartbeat to gateway."));

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