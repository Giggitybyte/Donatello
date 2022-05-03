﻿namespace Donatello.Interaction;

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Donatello;
using Donatello.Enumeration;
using Donatello.Interaction.Command.Module;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSec.Cryptography;
using Qmmands;
using Qommon.Events;

/// <summary>
/// Bot framework for Discord's interaction API model.<br/>
/// Interactions are received from Discord through an integrated webhook listener.
/// </summary>
public sealed class DiscordInteractionBot : DiscordApiBot
{
    private readonly PublicKey _publicKey;

    private CommandService _commandService;
    private Task _interactionListenerTask;
    private CancellationTokenSource _cts;
    private ushort _port;

    private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent;
    private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent;

    public DiscordInteractionBot(string apiToken, string publicKey, ushort port = 8080, ILogger logger = null) : base(apiToken, logger)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Token cannot be empty.", nameof(apiToken));
        else if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentException("Public key cannot be empty.", nameof(publicKey));

        this.Logger = logger ?? NullLogger.Instance;

        _publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, Convert.FromHexString(publicKey), KeyBlobFormat.PkixPublicKeyText);

        _commandExecutedEvent = new AsynchronousEvent<CommandExecutedEventArgs>(EventExceptionLogger);
        _commandExecutionFailedEvent = new AsynchronousEvent<CommandExecutionFailedEventArgs>(EventExceptionLogger);

        _commandService = new CommandService(CommandServiceConfiguration.Default);
        _commandService.CommandExecuted += (s, e) => _commandExecutedEvent.InvokeAsync(this, e);
        _commandService.CommandExecutionFailed += (s, e) => _commandExecutionFailedEvent.InvokeAsync(this, e);
    }

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

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Whether this instance is listening for interactions.</summary>
    public bool IsRunning => _interactionListenerTask.Status == TaskStatus.Running;

    /// <summary>Searches the provided assembly for classes which inherit from <see cref="DiscordCommandModule"/> and registers each of their commands.</summary>
    public void LoadCommandModules(Assembly assembly)
        => _commandService.AddModules(assembly);

    /// <summary>Registers all commands found in the provided command module type with the command framework.</summary>
    public void LoadCommandModule<T>() where T : DiscordCommandModule
        => _commandService.AddModule(typeof(T));

    /// <summary>Submits all registered commands to Discord and begins listening for interactions.</summary>
    public override ValueTask StartAsync()
    {
        if (this.IsRunning)
            throw new InvalidOperationException("Instance is already active.");

        _interactionListenerTask = InteractionListenerLoop(_cts.Token);

        foreach (var command in _commandService.GetAllCommands())
        {
            // ...
        }

        throw new NotImplementedException();
    }

    /// <summary>Stops listening for interactions.</summary>
    public override async ValueTask StopAsync()
    {
        if (!this.IsRunning)
            throw new InvalidOperationException("Instance is not active.");

        _cts.Cancel();

        await _interactionListenerTask;
        _interactionListenerTask.Dispose();
    }

    /// <summary>Webhook listener.</summary>
    private async Task InteractionListenerLoop(CancellationToken token)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"https://*:{_port}/");

        listener.Start();

        while (token.IsCancellationRequested is false)
        {
            var httpContext = await listener.GetContextAsync();
            var request = httpContext.Request;
            var response = httpContext.Response;

            using var streamReader = new StreamReader(request.InputStream, Encoding.UTF8);

            var data = await streamReader.ReadToEndAsync();
            var signature = request.Headers.Get("X-Signature-Ed25519");
            var timestamp = request.Headers.Get("X-Signature-Timestamp");

            bool isValidSignature = SignatureAlgorithm.Ed25519.Verify
            (
                _publicKey,
                Encoding.UTF8.GetBytes($"{timestamp}{data}"),
                Convert.FromHexString(signature)
            );

            if (isValidSignature)
                await ProcessInteractionAsync(data, response);
            else
            {
                response.StatusCode = 401;
                response.StatusDescription = "Invalid header signature.";
            }

            response.Close();
        }

        listener.Stop();


        async Task ProcessInteractionAsync(string stringData, HttpListenerResponse response)
        {
            using var payload = JsonDocument.Parse(stringData);
            var interactionJson = payload.RootElement;
            var interactionType = interactionJson.GetProperty("type").GetInt32();

            var responseBuffer = new ArrayBufferWriter<byte>();
            using var responseWriter = new Utf8JsonWriter(responseBuffer);

            if (interactionType == 1) // Ping
                responseWriter.WriteNumber("type", 1);
            else if (interactionType == 2) // Command
            {
                var data = interactionJson.GetProperty("data");
                var commandType = (CommandType) (data.TryGetProperty("type", out var prop) ? prop.GetInt32() : 1);

                var name = data.GetProperty("name").GetString();
                var result = _commandService.FindCommands(name)[0];

                if (result is not null)
                {
                    // ...
                }
                else
                {
                    response.StatusCode = 500;
                    response.StatusDescription = "Command not found.";
                }
            }
            else if (interactionType == 3) // Component
            {
                throw new NotImplementedException("Components");
            }
            else if (interactionType == 4) // Auto-complete
            {
                throw new NotImplementedException("Auto-complete");
            }
            else
            {
                response.StatusCode = 501;
                response.StatusDescription = "Unknown interaction type.";
                return;
            }


            if (responseWriter.BytesPending > 0)
            {
                await responseWriter.FlushAsync().ConfigureAwait(false);
                await response.OutputStream.WriteAsync(responseBuffer.WrittenMemory).ConfigureAwait(false);

                response.StatusCode = 200;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    private void EventExceptionLogger(Exception exception)
        => this.Logger.LogError(exception, "An event handler threw an exception.");

}
