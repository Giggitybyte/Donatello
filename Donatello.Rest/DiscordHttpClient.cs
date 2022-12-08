namespace Donatello.Rest;

using Donatello.Rest.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private ConcurrentDictionary<string, string> _routeBucketIds;
    private ConcurrentDictionary<string, RatelimitBucket> _routeBuckets;
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

        _routeBucketIds = new ConcurrentDictionary<string, string>();
        _routeBuckets = new ConcurrentDictionary<string, RatelimitBucket>();
        _globalBucket = new RatelimitBucket("global")
        {
            Limit = 50,
            Remaining = 50
        };

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Sends an HTTP request to an endpoint.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpRequest request)
        => this.SendRequestCoreAsync(request);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(Action<HttpRequest> requestDelegate)
    {
        var request = new HttpRequest();
        requestDelegate(request);

        return this.SendRequestCoreAsync(request);
    }

    /// <summary>Sends an HTTP request.</summary>
    private Task<HttpResponse> SendRequestCoreAsync(HttpRequest request)
    {
        ulong attemptCount = 0;
        Task<HttpResponse> delayTask = null;

        if (_globalBucket.TryUse())
        {
            if (_routeBucketIds.TryGetValue(request.Endpoint, out string bucketId))
                if (_routeBuckets.TryGetValue(bucketId, out RatelimitBucket routeBucket) && (routeBucket.TryUse() is false))
                    delayTask = DelayRequestAsync(routeBucket.ResetDate - DateTimeOffset.UtcNow);
        }
        else
            delayTask = DelayRequestAsync(_globalBucket.ResetDate - DateTimeOffset.UtcNow);

        return delayTask ?? DispatchRequestAsync();

        // Send request, update ratelimit bucket, handle 429, return response.
        async Task<HttpResponse> DispatchRequestAsync()
        {
            this.Logger.LogDebug("Sending request to {Method} {Uri} (attempt #{Attempt})", request.Method.Method, request.Endpoint, ++attemptCount);
            using var response = await _client.SendAsync(request);
            this.Logger.LogDebug("Request to {Url} got {Status} response from Discord.", request.Endpoint, response.StatusCode);

            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var values))
            {
                var bucketId = values.Single();
                _routeBucketIds[request.Endpoint] = bucketId;

                if (_routeBuckets.TryGetValue(bucketId, out var bucket))
                {
                    bucket.Update(response.Headers);
                    this.Logger.LogTrace("Updated existing ratelimit bucket {Id}", bucketId);
                }
                else
                {
                    bucket = new RatelimitBucket(bucketId, response.Headers);

                    _routeBuckets[bucketId] = bucket;
                    this.Logger.LogTrace("Created new ratelimit bucket {Id}", bucketId);
                }
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                var scope = response.Headers.GetValues("X-RateLimit-Scope").Single();
                var retrySeconds = int.Parse(response.Headers.GetValues("Retry-After").Single());
                var retryTime = TimeSpan.FromSeconds(retrySeconds);

                if (scope is "global")
                    this.Logger.LogCritical("Hit global rate limit");
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
            this.Logger.LogWarning("Request to {Uri} delayed until {Time} (attempt #{Attempt})", request.Endpoint, DateTime.Now.Add(delayTime), attemptCount);
            return Task.Delay(delayTime).ContinueWith(t => DispatchRequestAsync()).Unwrap();
        }

        // Returns all error messages present.
        ICollection<HttpResponse.Error> ParseErrorMessages(JsonElement json)
        {
            var errorMessages = new List<HttpResponse.Error>();

            // TODO: array error

            if (json.TryGetProperty("errors", out JsonElement errorObject))
            {
                if (errorObject.TryGetProperty("_errors", out JsonElement errorProp))
                    AddError(errorProp, "request");
                else
                {
                    foreach (var objectProp in errorObject.EnumerateObject())
                        foreach (var errorJson in objectProp.Value.GetProperty("_errors").EnumerateArray())
                            AddError(errorJson, objectProp.Name);
                }

                void AddError(JsonElement errorArray, string name)
                {
                    foreach (var errorJson in errorArray.EnumerateArray())
                    {
                        var error = new HttpResponse.Error()
                        {
                            ParameterName = name,
                            Code = errorJson.GetProperty("code").GetString(),
                            Message = errorJson.GetProperty("message").GetString()
                        };

                        errorMessages.Add(error);
                    }
                }
            }
            else if (json.TryGetProperty("message", out JsonElement messageProp))
            {
                var error = new HttpResponse.Error()
                {
                    ParameterName = string.Empty,
                    Code = json.GetProperty("code").GetString(),
                    Message = messageProp.GetString()
                };

                errorMessages.Add(error);
            }

            return errorMessages;
        }
    }
}