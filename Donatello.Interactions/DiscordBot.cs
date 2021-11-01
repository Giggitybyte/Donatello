namespace Donatello.Interactions;

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NSec.Cryptography;
using Qmmands;
using Qommon.Events;

/// <summary>
/// High-level client and bot framework for the Discord interaction model.<br/>
/// Interactions are received from Discord through an integrated webhook listener.
/// </summary>
public sealed class DiscordBot
{
    private readonly string _apiToken;
    private readonly PublicKey _publicKey;

    private Task _interactionListenerTask;
    private CommandService _commandService;
    private CancellationTokenSource _cts;

    private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent = new(EventExceptionLogger);
    private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent = new(EventExceptionLogger);

    public DiscordBot(string apiToken, string publicKey)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Token cannot be empty.", nameof(apiToken));
        else if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentException("Public key cannot be empty.", nameof(publicKey));

        _apiToken = apiToken;
        _publicKey = PublicKey.Import
        (
            SignatureAlgorithm.Ed25519,
            Convert.FromHexString(publicKey),
            KeyBlobFormat.PkixPublicKeyText
        );

        _commandService = new CommandService(CommandServiceConfiguration.Default);
        _commandService.CommandExecuted += (e) => _commandExecutedEvent.InvokeAsync(e);
        _commandService.CommandExecutionFailed += (e) => _commandExecutionFailedEvent.InvokeAsync(e);
    }

    /// <summary>Whether this instance is listening for interactions.</summary>
    public bool IsRunning { get => _interactionListenerTask.Status == TaskStatus.Running; }

    /// <summary>Fired when a command successfully executes.</summary>
    public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
    {
        add => _commandExecutedEvent.Hook(value);
        remove => _commandExecutedEvent.Unhook(value);
    }

    /// <summary>Fired when a command cannot complete execution.</summary>
    public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
    {
        add => _commandExecutionFailedEvent.Hook(value);
        remove => _commandExecutionFailedEvent.Unhook(value);
    }

    /// <summary>Submits all registered commands to Discord and begins listening for interactions.</summary>
    public async ValueTask StartAsync(int port = 8080)
    {
        if (this.IsRunning)
            throw new InvalidOperationException("Instance is already active.");

        _interactionListenerTask = InteractionListenerLoop(port, _cts.Token);

        foreach (var command in _commandService.GetAllCommands())
        {
            // ...
        }
    }

    /// <summary>Stops listening for interactions.</summary>
    public async ValueTask StopAsync()
    {
        if (!this.IsRunning)
            throw new InvalidOperationException("Instance is not active.");

        _cts.Cancel();

        await _interactionListenerTask;
        _interactionListenerTask.Dispose();
    }

    /// <summary>Webhook listener.</summary>
    private async Task InteractionListenerLoop(int port, CancellationToken token)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"https://*:{port}/");

        listener.Start();

        while (!token.IsCancellationRequested)
        {
            var httpContext = await listener.GetContextAsync().ConfigureAwait(false);
            var request = httpContext.Request;
            var response = httpContext.Response;

            using var streamReader = new StreamReader(request.InputStream, Encoding.UTF8);

            var data = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            var signature = request.Headers.Get("X-Signature-Ed25519");
            var timestamp = request.Headers.Get("X-Signature-Timestamp");

            bool isValidSignature = SignatureAlgorithm.Ed25519.Verify
            (
                _publicKey,
                Encoding.UTF8.GetBytes($"{timestamp}{data}"),
                Convert.FromHexString(signature)
            );

            if (isValidSignature)
                await ProcessInteractionAsync(data, response).ConfigureAwait(false);
            else
            {
                response.StatusCode = 401;
                response.StatusDescription = "Invalid header signature.";
            }

            response.Close();
        }

        listener.Stop();
    }

    /// <summary>Interaction client implementation.</summary>
    private async Task ProcessInteractionAsync(string rawData, HttpListenerResponse response)
    {
        using var payload = JsonDocument.Parse(rawData);
        var interactionType = payload.RootElement.GetProperty("type").GetInt32();

        var responseBuffer = new ArrayBufferWriter<byte>();
        using var responseWriter = new Utf8JsonWriter(responseBuffer);

        {
            if (interactionType == 1) // Ping
                responseWriter.WriteNumber("type", 1);

            else if (interactionType == 2) // Command
            {
                var data = payload.RootElement.GetProperty("data");
                var commandType = data.TryGetProperty("type", out var prop) ? prop.GetInt32() : 1;

                var name = data.GetProperty("name").GetString();
                var result = _commandService.FindCommands(name).FirstOrDefault();

                if (result is not null)
                {
                    // Execute and return response.
                }
                else
                {
                    response.StatusCode = 500;
                    response.StatusDescription = "Command not found.";
                }
            }

            else if (interactionType == 3) // Component
            {
                // ...
            }

            else
            {
                response.StatusCode = 501;
                response.StatusDescription = "Unknown interaction type.";
                return;
            }
        }

        if (responseWriter.BytesPending > 0)
        {
            await responseWriter.FlushAsync().ConfigureAwait(false);
            await response.OutputStream.WriteAsync(responseBuffer.WrittenMemory).ConfigureAwait(false);

            response.StatusCode = 200;
        }
    }

    private static Task EventExceptionLogger(Exception exception)
    {
        // this.Logger.Log(...);
        throw new NotImplementedException();
    }
}
