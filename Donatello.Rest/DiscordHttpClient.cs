namespace Donatello.Rest;

using Donatello.Rest.Extensions.Json;
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
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (creator: thegiggitybyte#8099)");
    }

    /// <summary></summary>
    public DiscordHttpClient(string token, bool isBearer)
    {
        _authHeader = new("Authorization", $"{(isBearer ? "Bearer" : "Bot")} {token}");
        _requestBuckets = new ConcurrentDictionary<string, RequestBucket>();
    }

    /// <summary></summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint)
        => SendRequestCoreAsync(method, endpoint);

    /// <summary>Requests</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonBuilder)
        => SendRequestCoreAsync(method, endpoint, jsonBuilder.ToContent());

    /// <summary></summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonParams, IEnumerable<Stream> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonParams, attachments.Select(s => new StreamContent(s)));

    /// <summary></summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonParams, IEnumerable<byte[]> attachments)
        => SendMultipartRequestAsync(method, endpoint, jsonParams, attachments.Select(b => new ByteArrayContent(b)));

    /// <summary></summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, IEnumerable<Stream> attachments)
        => SendMultipartRequestAsync(method, endpoint, contents: attachments.Select(s => new StreamContent(s)));

    /// <summary></summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, IEnumerable<byte[]> attachments)
        => SendMultipartRequestAsync(method, endpoint, contents: attachments.Select(b => new ByteArrayContent(b)));

    /// <summary></summary>
    private Task<HttpResponse> SendMultipartRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonParams = null, IEnumerable<HttpContent> contents = null)
    {
        var multipartContent = new MultipartFormDataContent();

        if (jsonParams is not null)
            multipartContent.Add(jsonParams.ToContent());

        foreach (var content in contents)
            multipartContent.Add(content);

        return SendRequestCoreAsync(method, endpoint, multipartContent);
    }

    /// <summary></summary>
    private async Task<HttpResponse> SendRequestCoreAsync(HttpMethod method, string endpoint, HttpContent content = null)
    {
        endpoint = endpoint.Trim('/');

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Add(_authHeader.Key, _authHeader.Value);

        if (content is not null)
            request.Content = content;

        if (IsRatelimited(endpoint, out var delayTime))
            return await DelayRequestAsync(request, delayTime);
        else
            return await ExecuteRequestAsync(request);

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

        if (response.StatusCode == HttpStatusCode.TooManyRequests) // Ratelimited.
        {
            var retrySeconds = responseJson.RootElement.GetProperty("retry_after").GetSingle();
            var retryTime = TimeSpan.FromSeconds(retrySeconds);

            if (responseJson.RootElement.GetProperty("global").GetBoolean()) // Global ratelimit.
                _globalRatelimitResetDate = DateTime.Now + retryTime;
            else // Request ratelimit.
                return await DelayRequestAsync(request, retryTime);

            // TODO: Resource ratelimit.
        }

        return new HttpResponse()
        {
            Status = response.StatusCode,
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

