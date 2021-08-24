using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Donatello.Interactions.Commands;
using NSec.Cryptography;
using Qmmands;
using Qommon.Events;

namespace Donatello.Interactions
{
    public sealed class DiscordBot
    {
        internal static readonly HttpClient HttpClient = new HttpClient();

        private readonly string _apiToken;
        private readonly PublicKey _publicKey;

        private Task _httpListenerTask;
        private CommandService _commandService;
        private CancellationTokenSource _cts;

        private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent = new(EventExceptionLogger);
        private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent = new(EventExceptionLogger);

        /// <summary>Fired when a command successfully executes.</summary>
        public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
        {
            add => _commandExecutedEvent.Hook(value);
            remove => _commandExecutedEvent.Unhook(value);
        }

        /// <summary>Fired when a command does not</summary>
        public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
        {
            add => _commandExecutionFailedEvent.Hook(value);
            remove => _commandExecutionFailedEvent.Unhook(value);
        }

        public DiscordBot(string apiToken, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                throw new ArgumentException("Token cannot be empty.", nameof(apiToken));
            else if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Public key cannot be empty.", nameof(publicKey));

            _apiToken = apiToken;
            _publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, Convert.FromHexString(publicKey), KeyBlobFormat.PkixPublicKeyText);

            
            _commandService.CommandExecuted += (e) => _commandExecutedEvent.InvokeAsync(e);
            _commandService.CommandExecutionFailed += (e) => _commandExecutionFailedEvent.InvokeAsync(e);
        }

        public bool IsRunning { get => _httpListenerTask.Status == TaskStatus.Running; }

        public async ValueTask StartAsync(int port = 8080)
        {
            if (this.IsRunning)
                throw new InvalidOperationException("Instance is already active.");

            _httpListenerTask = HttpListenerLoop(port, _cts.Token);
        }

        public async ValueTask StopAsync()
        {
            if (!this.IsRunning)
                throw new InvalidOperationException("Instance is not active.");

            _cts.Cancel();
            await _httpListenerTask;
            _httpListenerTask.Dispose();
        }

        private async Task HttpListenerLoop(int port, CancellationToken token)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"https://*:{port}/");

            listener.Start();

            while (!token.IsCancellationRequested)
            {
                var httpContext = await listener.GetContextAsync().ConfigureAwait(false);
                var request = httpContext.Request;

                using var streamReader = new StreamReader(request.InputStream, Encoding.UTF8);

                var data = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                var signature = request.Headers.Get("X-Signature-Ed25519");
                var timestamp = request.Headers.Get("X-Signature-Timestamp");

                var outgoingResponse = httpContext.Response;
                var isValidSignature = SignatureAlgorithm.Ed25519.Verify
                (
                    _publicKey,
                    Encoding.UTF8.GetBytes($"{timestamp}{data}"),
                    Convert.FromHexString(signature)
                );

                if (isValidSignature)
                    await ProcessRequestAsync(data, outgoingResponse).ConfigureAwait(false);
                else
                {
                    outgoingResponse.StatusCode = 401;
                    outgoingResponse.StatusDescription = "Invalid header signature.";
                }

                outgoingResponse.Close();
            }

            listener.Stop();
        }

        async Task ProcessRequestAsync(string data, HttpListenerResponse response)
        {
            using var payload = JsonDocument.Parse(data);
            var interactionType = payload.RootElement.GetProperty("type").GetInt32();

            var responseBuffer = new ArrayBufferWriter<byte>();
            using var responseWriter = new Utf8JsonWriter(responseBuffer);

            if (interactionType == 1) // Ping
            {
                responseWriter.WriteNumber("type", 1);
            }

            else if (interactionType == 2) // Command
            {
                var executionTask = _commandService.ExecuteAsync("", new DiscordCommandContext());
                var deferTimerTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(deferTimerTask, executionTask).ConfigureAwait(false);

                if (completedTask == executionTask)
                {
                    var result = await executionTask;
                    result.
                }
                else
                {
                    responseWriter.WriteNumber("type", 5);
                }
            }

            else if (interactionType == 3) // Component
            {

            }

            else
            {
                response.StatusCode = 501;
                response.StatusDescription = "Unknown interaction type.";

                return;
            }

            await responseWriter.FlushAsync().ConfigureAwait(false);
            await response.OutputStream.WriteAsync(responseBuffer.WrittenMemory).ConfigureAwait(false);

            response.StatusCode = 200;
        }

        private static Task EventExceptionLogger(Exception exception)
        {
            // this.Logger.Log(...);
            throw new NotImplementedException();
        }
    }
}
