namespace Donatello.Rest;

using Donatello.Rest.Extension.Internal;
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

/// <summary>HTTP client wrapper for the Discord REST API.</summary>
public class DiscordHttpClient
{
    private static HttpClient _client;
    private DateTime _globalRatelimitResetDate;
    private ConcurrentDictionary<Uri, string> _ratelimitBucketIds;
    private ConcurrentDictionary<string, RatelimitBucket> _ratelimitBuckets;

    static DiscordHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://discord.com/api/v10");
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (creator: 176477523717259264)");
    }

    /// <param name="token">Discord token.</param>
    /// <param name="isBearerToken"><see langword="true"/>: OAuth2 bearer token.<br/><see langword="false"/>: application bot token.</param>
    public DiscordHttpClient(string token, ILogger logger = null, bool isBearerToken = false)
    {
        _client.DefaultRequestHeaders.Add("Authorization", $"{(isBearerToken ? "Bearer" : "Bot")} {token}");

        _ratelimitBucketIds = new ConcurrentDictionary<Uri, string>();
        _ratelimitBuckets = new ConcurrentDictionary<string, RatelimitBucket>();

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Sends an HTTP request to an endpoint.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint)
        => SendRequestCoreAsync(method, endpoint);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonDelegate)
        => SendRequestCoreAsync(method, endpoint, jsonDelegate.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject)
        => SendRequestCoreAsync(method, endpoint, jsonObject.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonDelegate, IList<Attachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonDelegate.ToContent(), attachments);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject, IList<Attachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonObject.ToContent(), attachments);

    /// <summary>Sends a multi-part HTTP request to an endpoint.</summary>
    private Task<HttpResponse> SendMultipartRequestAsync(HttpMethod method, string endpoint, StringContent jsonContent, IList<Attachment> attachments)
    {
        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(jsonContent, "payload_json");

        for (int index = 0; index < attachments.Count; index++)
        {
            var attachment = attachments[index];
            multipartContent.Add(attachment.Content, $"files[{index}]", attachment.Name);
        }

        return SendRequestCoreAsync(method, endpoint, multipartContent);
    }

    /// <summary>Sends an HTTP request.</summary>
    private Task<HttpResponse> SendRequestCoreAsync(HttpMethod method, string endpoint, HttpContent content = null)
    {
        var attemptCount = 0;
        var requestDate = DateTime.Now;
        var request = new HttpRequestMessage(method, endpoint.Trim('/'));

        if (content is not null)
            request.Content = content;

        if (_globalRatelimitResetDate > requestDate)
            return DelayRequestAsync(_globalRatelimitResetDate - requestDate);

        if (_ratelimitBucketIds.TryGetValue(request.RequestUri, out var bucketId))
            if (_ratelimitBuckets.TryGetValue(bucketId, out var bucket))
                if (bucket.TryUse() is false)
                    return DelayRequestAsync(bucket.ResetDate - requestDate);

        return DispatchRequestAsync();

        async Task<HttpResponse> DelayRequestAsync(TimeSpan delayTime)
        {
            this.Logger.LogWarning("Request to {Uri} delayed until {Time} (attempt #{Attempt})", request.RequestUri.AbsolutePath, DateTime.Now.Add(delayTime), attemptCount);

            await Task.Delay(delayTime);
            return await DispatchRequestAsync();
        }

        async Task<HttpResponse> DispatchRequestAsync()
        {
            this.Logger.LogDebug("Sending {Method} request to {Uri} (attempt #{Attempt})", request.Method.Method, request.RequestUri, ++attemptCount);
            using var response = await _client.SendAsync(request);
            this.Logger.LogDebug("Request to {Url} got {Status} response from Discord.", request.RequestUri.AbsolutePath, response.StatusCode);

            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var headers))
            {
                var bucketId = headers.FirstOrDefault();
                if (_ratelimitBuckets.TryGetValue(bucketId, out var bucket))
                {
                    bucket.Update(response.Headers);
                    this.Logger.LogTrace("Updated existing ratelimit bucket {Id}", bucketId);
                }
                else
                {
                    _ratelimitBucketIds.TryAdd(request.RequestUri, bucketId);

                    bucket = new RatelimitBucket(response.Headers);
                    _ratelimitBuckets.TryAdd(bucketId, bucket);
                    this.Logger.LogTrace("Created new ratelimit bucket {Id}", bucketId);
                }
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                var scope = response.Headers.GetValues("X-RateLimit-Scope").Single();
                var retrySeconds = int.Parse(response.Headers.GetValues("Retry-After").Single());
                var retryTime = TimeSpan.FromSeconds(retrySeconds);

                if (scope is "global")
                {
                    _globalRatelimitResetDate = DateTime.Now + retryTime;
                    this.Logger.LogCritical("Hit global rate limit");
                }
                else
                    this.Logger.LogWarning("Hit {Scope} ratelimit for {Url}", scope, request.RequestUri.AbsolutePath);

                return await DelayRequestAsync(retryTime);
            }
            else
            {
                using var responsePayload = await response.Content.ReadAsStreamAsync();
                using var responseJson = await JsonDocument.ParseAsync(responsePayload);

                var timeElapsed = DateTime.Now.Subtract(requestDate).TotalMilliseconds;
                this.Logger.LogTrace("Completed request to {Url} after {Attempt} attempts over {Time}ms", request.RequestUri.AbsolutePath, attemptCount, timeElapsed);

                return new HttpResponse()
                {
                    Status = response.StatusCode,
                    Message = response.ReasonPhrase,
                    Payload = responseJson.RootElement.Clone()
                };
            }
        }
    }
}

