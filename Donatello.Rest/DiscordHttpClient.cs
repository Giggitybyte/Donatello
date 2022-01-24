namespace Donatello.Rest;

using Donatello.Rest.Transport;
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
    private ConcurrentDictionary<Uri, string> _bucketIds;
    private ConcurrentDictionary<string, RequestBucket> _requestBuckets;
    private KeyValuePair<string, string> _authHeader;
    private DateTime _globalRatelimitResetDate;

    static DiscordHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://discord.com/api/v9");
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (creator: 176477523717259264)");
    }

    /// <param name="token">Discord token.</param>
    /// <param name="isBearerToken"><see langword="true"/>: OAuth2 bearer token.<br/><see langword="false"/>: application bot token.</param>
    public DiscordHttpClient(string token, bool isBearerToken = false, ILogger logger = null)
    {
        _authHeader = new("Authorization", $"{(isBearerToken ? "Bearer" : "Bot")} {token}");
        _bucketIds = new ConcurrentDictionary<Uri, string>();
        _requestBuckets = new ConcurrentDictionary<string, RequestBucket>();

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Sends an HTTP request to an endpoint.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint)
        => SendRequestCoreAsync(method, endpoint);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder)
        => SendRequestCoreAsync(method, endpoint, jsonBuilder?.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject)
        => SendRequestCoreAsync(method, endpoint, jsonObject.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder, IList<FileAttachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonBuilder?.ToContent(), attachments);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject, IList<FileAttachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonObject.ToContent(), attachments);

    /// <summary>Sends a multi-part HTTP request to an endpoint.</summary>
    private Task<HttpResponse> SendMultipartRequestAsync(HttpMethod method, string endpoint, StringContent jsonContent, IList<FileAttachment> attachments)
    {
        var multipartContent = new MultipartFormDataContent()
        {
            { jsonContent, "payload_json" }
        };

        for (int index = 0; index < attachments.Count; index++)
        {
            var attachment = attachments[index];
            multipartContent.Add(attachment.Content, $"files[{index}]", attachment.Name);
        }

        return SendRequestCoreAsync(method, endpoint, multipartContent);
    }

    /// <summary>Sends an HTTP request.</summary>
    private async Task<HttpResponse> SendRequestCoreAsync(HttpMethod method, string endpoint, HttpContent content = null)
    {
        endpoint = endpoint.Trim('/');

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Add(_authHeader.Key, _authHeader.Value);

        if (content is not null)
            request.Content = content;

        // Should we apply a delay to sending this request?
        var currentDate = DateTime.Now;
        var delayTime = TimeSpan.Zero;

        if (_globalRatelimitResetDate > currentDate)
            delayTime = _globalRatelimitResetDate - currentDate;
        else if (_bucketIds.TryGetValue(request.RequestUri, out var bucketId) && _requestBuckets.TryGetValue(bucketId, out var bucket) && bucket.TryUse())
            delayTime = bucket.ResetDate - currentDate;

        if (delayTime == TimeSpan.Zero) // No.
            return await ExecuteRequestAsync(request).ConfigureAwait(false);
        else // Yes.
            return await DelayRequestAsync(request, delayTime).ConfigureAwait(false);


        async Task<HttpResponse> ExecuteRequestAsync(HttpRequestMessage request)
        {
            this.Logger.LogTrace("Sending {Method} request to {Uri}", request.Method.Method, request.RequestUri);

            using var response = await _client.SendAsync(request).ConfigureAwait(false);
            using var responsePayload = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var responseJson = await JsonDocument.ParseAsync(responsePayload).ConfigureAwait(false);


            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var headers)) // Update bucket
            {
                var bucketId = headers.FirstOrDefault();
                if (_requestBuckets.TryGetValue(bucketId, out var bucket))
                {
                    bucket.Update(response.Headers);
                    this.Logger.LogTrace("Updated existing ratelimit bucket for {Uri}", request.RequestUri.AbsolutePath);
                }
                else
                {
                    _bucketIds.TryAdd(request.RequestUri, bucketId);

                    bucket = new RequestBucket(response.Headers);
                    _requestBuckets.TryAdd(bucketId, bucket);
                    this.Logger.LogTrace("Created new ratelimit bucket for {Uri}", request.RequestUri.AbsolutePath);
                }
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests) // Handle rate limit
            {
                var retrySeconds = int.Parse(response.Headers.GetValues("Retry-After").First());
                var retryTime = TimeSpan.FromSeconds(retrySeconds);

                var scope = response.Headers.GetValues("X-RateLimit-Scope").First();
                var message = string.Empty;

                if (scope is "global")
                {
                    _globalRatelimitResetDate = DateTime.Now + retryTime;
                    this.Logger.LogCritical("Hit global rate limit");
                }
                else if (scope is "user" or "shared")
                    this.Logger.LogWarning("Hit {Scope} ratelimit for {Uri}", scope, request.RequestUri.AbsolutePath);
                else
                    this.Logger.LogWarning("Unknown ratelimit scope '{Scope}'", scope);

                return await DelayRequestAsync(request, retryTime).ConfigureAwait(false);
            }

            return new HttpResponse()
            {
                Status = response.StatusCode,
                Message = response.ReasonPhrase,
                Payload = responseJson.RootElement.Clone()
            };
        }

        async Task<HttpResponse> DelayRequestAsync(HttpRequestMessage request, TimeSpan delayTime)
        {
            this.Logger.LogDebug("Request to {Uri} delayed until {Time}", request.RequestUri.AbsolutePath, DateTime.Now + delayTime);

            await Task.Delay(delayTime);
            return await ExecuteRequestAsync(request);
        }
    }
}

