namespace Donatello.Rest;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>HTTP client for the Discord REST API.</summary>
public class DiscordHttpClient
{
    private static HttpClient _client;

    private ConcurrentDictionary<Uri, RequestBucket> _requestBuckets;
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
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello.Rest 0.0.1 - thegiggitybyte#8099");
    }

    public DiscordHttpClient(string token, bool isBearer)
    {
        _authHeader = new("Authorization", $"{(isBearer ? "Bearer" : "Bot")} {token}");
        _requestBuckets = new ConcurrentDictionary<Uri, RequestBucket>();
    }

    /// <summary></summary>
    public async Task<HttpResponse> SendRequestAsync(HttpMethod method, Uri endpoint, string payload = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Add(_authHeader.Key, _authHeader.Value);

        if (payload is not null)
            request.Content = JsonContent.Create(payload);

        if (IsRatelimited(endpoint, out var delayTime))
            return await DelayRequestAsync(request, delayTime);
        else
            return await ExecuteRequestAsync(request);


        bool IsRatelimited(Uri endpoint, out TimeSpan delayTime)
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

    private async Task<HttpResponse> ExecuteRequestAsync(HttpRequestMessage request)
    {
        var response = await _client.SendAsync(request).ConfigureAwait(false);
        using var responsePayload = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var responseJson = await JsonDocument.ParseAsync(responsePayload).ConfigureAwait(false);

        if (_requestBuckets.TryGetValue(request.RequestUri, out var existingBucket))
            existingBucket.Update(response.Headers);
        else if (RequestBucket.TryParse(response.Headers, out var newBucket))
            _requestBuckets.TryAdd(request.RequestUri, newBucket);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retrySeconds = responseJson.RootElement.GetProperty("retry_after").GetSingle();
            var retryTime = TimeSpan.FromSeconds(retrySeconds);

            if (responseJson.RootElement.GetProperty("global").GetBoolean())
                _globalRatelimitResetDate = DateTime.Now + retryTime;
            else
                return await DelayRequestAsync(request, retryTime); // make this better?
        }

        return new HttpResponse()
        {
            Status = response.StatusCode,
            Payload = responseJson.RootElement.Clone()
        };
    }

    private async Task<HttpResponse> DelayRequestAsync(HttpRequestMessage request, TimeSpan delayTime)
    {
        await Task.Delay(delayTime).ConfigureAwait(false);
        return await ExecuteRequestAsync(request).ConfigureAwait(false);
    }
}

