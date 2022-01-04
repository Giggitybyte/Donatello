namespace Donatello.Rest;

using Donatello.Rest.Extension.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>HTTP client wrapper for the Discord REST API.</summary>
public class DiscordHttpClient
{
    private static HttpClient _client;

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
        _requestBuckets = new ConcurrentDictionary<string, RequestBucket>();

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Sends an HTTP request an endpoint.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint)
        => SendRequestCoreAsync(method, endpoint);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder)
        => SendRequestCoreAsync(method, endpoint, jsonBuilder?.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject)
        => SendRequestCoreAsync(method, endpoint, jsonObject.ToContent());

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder, IEnumerable<Stream> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonBuilder, attachments.Select(s => new StreamContent(s)));

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder, IEnumerable<byte[]> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonBuilder, attachments.Select(b => new ByteArrayContent(b)));

    /// <summary>Sends an HTTP request to an endpoint with file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, IEnumerable<Stream> attachments)
        => SendMultipartRequestAsync(method, endpoint, contents: attachments.Select(s => new StreamContent(s)));

    /// <summary>Sends an HTTP request to an endpoint with file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, IEnumerable<byte[]> attachments)
        => SendMultipartRequestAsync(method, endpoint, contents: attachments.Select(b => new ByteArrayContent(b)));

    /// <summary>Sends a multi-part HTTP request to an endpoint.</summary>
    private Task<HttpResponse> SendMultipartRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonParams = null, IEnumerable<HttpContent> contents = null)
    {
        var multipartContent = new MultipartFormDataContent();

        if (jsonParams is not null)
            multipartContent.Add(jsonParams.ToContent());

        foreach (var content in contents)
            multipartContent.Add(content);

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

        bool IsRatelimited(string endpoint, out TimeSpan delayTime)
        {
            if (_globalRatelimitResetDate > DateTime.Now)
            {
                delayTime = DateTime.Now - _globalRatelimitResetDate;
                return true;
            }

            _requestBuckets.TryGetValue(endpoint, out var bucket);

            if (bucket is null || bucket.TryUse())
            {
                delayTime = TimeSpan.Zero;
                return false;
            }
            else
            {
                delayTime = DateTime.Now - bucket.ResetTime;
                return true;
            }
        }

        if (IsRatelimited(endpoint, out var delayTime))
            return await DelayRequestAsync(request, delayTime).ConfigureAwait(false);
        else
            return await ExecuteRequestAsync(request).ConfigureAwait(false);
    }

    /// <summary></summary>
    private async Task<HttpResponse> ExecuteRequestAsync(HttpRequestMessage request)
    {
        var response = await _client.SendAsync(request).ConfigureAwait(false);

        using var responsePayload = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var responseJson = await JsonDocument.ParseAsync(responsePayload).ConfigureAwait(false);

        if (_requestBuckets.TryGetValue(request.RequestUri.ToString(), out var existingBucket))
            existingBucket.Update(response.Headers);
        else if (RequestBucket.TryParse(response.Headers, out var newBucket))
            _requestBuckets.TryAdd(request.RequestUri.ToString(), newBucket);

        if (response.StatusCode is HttpStatusCode.TooManyRequests) // Ratelimited
        {
            var retrySeconds = int.Parse(response.Headers.GetValues("X-RateLimit-Reset-After").First());
            var retryTime = TimeSpan.FromSeconds(retrySeconds);

            var scope = response.Headers.GetValues("X-RateLimit-Scope").First();

            if (scope is "global")
                _globalRatelimitResetDate = DateTime.Now + retryTime;
            else if (scope is "shared" or "user")
                return await DelayRequestAsync(request, retryTime).ConfigureAwait(false);
            else
                throw new NotImplementedException();
        }
        else if (response.StatusCode is not HttpStatusCode.OK or HttpStatusCode.NoContent)
            throw new DiscordHttpException(response.StatusCode, response.ReasonPhrase, responseJson.RootElement.Clone());

        return new HttpResponse()
        {
            Status = response.StatusCode,
            Message = response.ReasonPhrase,
            Payload = responseJson.RootElement.Clone()
        };
    }

    /// <summary></summary>
    private async Task<HttpResponse> DelayRequestAsync(HttpRequestMessage request, TimeSpan delayTime)
    {
        await Task.Delay(delayTime).ConfigureAwait(false);
        return await ExecuteRequestAsync(request).ConfigureAwait(false);
    }
}

