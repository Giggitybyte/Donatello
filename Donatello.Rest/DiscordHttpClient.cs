namespace Donatello.Rest;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public enum TokenType : ushort
{
    /// <summary></summary>
    Bot,

    /// <summary></summary>
    Bearer
}

/// <summary>HTTP client wrapper for the Discord REST API with integrated rate-limiter.</summary>
public class DiscordHttpClient
{
    private HttpClient _client;
    private ConcurrentDictionary<string, string> _endpointBucketIds;
    private ConcurrentDictionary<string, RatelimitBucket> _endpointBuckets;
    private RatelimitBucket _globalBucket;

    /// <param name="token">Discord token.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="tokenType"><see langword="true"/>: OAuth2 bearer token.<br/><see langword="false"/>: application bot token.</param>
    public DiscordHttpClient(TokenType tokenType, string token, ILogger logger = null)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://discord.com/api/v10");
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (author: 176477523717259264)");
        _client.DefaultRequestHeaders.Add("Authorization", $"{(tokenType is TokenType.Bot ? "Bot" : "Bearer")} {token}");

        _endpointBucketIds = new ConcurrentDictionary<string, string>();
        _endpointBuckets = new ConcurrentDictionary<string, RatelimitBucket>();
        _globalBucket = new RatelimitBucket("global") { Limit = 50, Remaining = 50 };

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <inheritdoc cref="SendRequestCoreAsync(HttpRequest)"/>
    public Task<HttpResponse> SendRequestAsync(HttpRequest request)
        => this.SendRequestCoreAsync(request);

    /// <inheritdoc cref="SendRequestCoreAsync(HttpRequest)"/>
    public Task<HttpResponse> SendRequestAsync(Action<HttpRequest> requestDelegate)
    {
        var request = new HttpRequest();
        requestDelegate(request);

        return this.SendRequestCoreAsync(request);
    }

    /// <inheritdoc cref="SendRequestCoreAsync(HttpRequest)"/>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonDelegate = null, IList<FileAttachment> attachments = null)
    {
        return this.SendRequestAsync(request =>
        {
            request.SetMethod(method);
            request.SetEndpoint(endpoint);

            if (jsonDelegate is not null)
                request.WriteJson(jsonDelegate);

            if (attachments is not null)
                foreach (var attachment in attachments)
                    request.AppendFile(attachment);
        });
    }

    /// <summary>Sends an HTTP request to an endpoint.</summary>
    private Task<HttpResponse> SendRequestCoreAsync(HttpRequest request)
    {
        ulong attemptCount = 0;

        if (_globalBucket.TryUse() is false)
            return DelayRequestAsync(_globalBucket.ResetDate - DateTimeOffset.UtcNow);

        if (_endpointBucketIds.TryGetValue(request.Endpoint, out string bucketId) &&
            _endpointBuckets.TryGetValue(bucketId, out RatelimitBucket endpointBucket) &&
            endpointBucket.TryUse() is false)
        {
            this.Logger.LogWarning("Request to {Endpoint} will be delayed to avoid exceeding ratelimits.", request.Endpoint);
            return DelayRequestAsync(endpointBucket.ResetDate - DateTimeOffset.UtcNow);
        }

        return DispatchRequestAsync();


        // Send request, update ratelimit bucket, handle 429, return response.
        async Task<HttpResponse> DispatchRequestAsync()
        {
            this.Logger.LogTrace("Sending request to {Method} {Uri} (attempt #{Attempt})", request.Method.Method, request.Endpoint, ++attemptCount);
            using var response = await _client.SendAsync(request);
            this.Logger.LogDebug("Request to {Endpoint} got {Status} response from Discord.", request.Endpoint, response.StatusCode);

            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var values))
            {
                var bucketId = values.Single();
                _endpointBucketIds[request.Endpoint] = bucketId;

                if (_endpointBuckets.TryGetValue(bucketId, out RatelimitBucket bucket))
                    bucket.Update(response.Headers);
                else
                {
                    bucket = new RatelimitBucket(bucketId, response.Headers);
                    _endpointBuckets[bucketId] = bucket;
                }

                this.Logger.LogTrace("Mapped {Endpoint} to {Id}", request.Endpoint, bucketId);
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                var scope = response.Headers.GetValues("X-RateLimit-Scope").Single();
                var retrySeconds = int.Parse(response.Headers.GetValues("Retry-After").Single());
                var retryTime = TimeSpan.FromSeconds(retrySeconds);

                if (scope is "global")
                    this.Logger.LogCritical("Hit global rate limit!");
                else
                    this.Logger.LogWarning("Hit {Scope} ratelimit for {Url}", scope, request.Endpoint);

                return await DelayRequestAsync(retryTime);
            }

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var responseJson = await JsonDocument.ParseAsync(responseStream);

            this.Logger.LogTrace("Request to {Url} completed after {Attempt} attempts.", request.Endpoint, attemptCount);

            return new HttpResponse()
            {
                Errors = ParseErrorMessages(responseJson.RootElement),
                Payload = responseJson.RootElement.Clone(),
                Status = response.StatusCode,
                Message = response.ReasonPhrase
            };
        }

        // Retries the request at a different time.
        Task<HttpResponse> DelayRequestAsync(TimeSpan delayTime)
        {
            this.Logger.LogWarning("Delayed request to {Uri} until {Time} (attempt #{Attempt})", request.Endpoint, DateTime.Now.Add(delayTime), attemptCount);
            return Task.Delay(delayTime).ContinueWith(t => DispatchRequestAsync()).Unwrap();
        }

        // Returns all error messages present.
        ICollection<HttpResponse.Error> ParseErrorMessages(JsonElement json)
        {
            var errorMessages = new List<HttpResponse.Error>();

            if (json.ValueKind is JsonValueKind.Object)
            {
                // TODO: array error.

                if (json.TryGetProperty("errors", out JsonElement errorObject)) // TODO: I think this logic is broke a little.
                {
                    if (errorObject.TryGetProperty("_errors", out JsonElement errorProp))
                        AddErrors(errorProp, "request");
                    else
                    {
                        foreach (var objectProp in errorObject.EnumerateObject())
                            foreach (var errorJson in objectProp.Value.GetProperty("_errors").EnumerateArray())
                                AddErrors(errorJson, objectProp.Name);
                    }
                }
                else if (json.TryGetProperty("message", out JsonElement messageProp))
                {
                    var error = new HttpResponse.Error()
                    {
                        ParameterName = string.Empty,
                        Code = json.GetProperty("code").GetInt32(),
                        Message = messageProp.GetString()
                    };

                    errorMessages.Add(error);
                }
            }

            return errorMessages;

            void AddErrors(JsonElement errorArray, string name)
            {
                foreach (var errorJson in errorArray.EnumerateArray())
                {
                    var error = new HttpResponse.Error()
                    {
                        ParameterName = name,
                        Code = errorJson.GetProperty("code").GetInt32(),
                        Message = errorJson.GetProperty("message").GetString()
                    };

                    errorMessages.Add(error);
                }
            }
        }
    }
}