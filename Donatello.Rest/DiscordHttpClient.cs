using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Donatello.Rest
{
    /// <summary>Simple HTTP client for the Discord REST API.</summary>
    internal class DiscordHttpClient
    {
        private static HttpClient _client;

        private KeyValuePair<string, string> _authHeader;
        private ConcurrentDictionary<Uri, RequestBucket> _requestBuckets;
        private DateTime? _globalRatelimitResetTime;

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

        internal DiscordHttpClient(string token, bool isBearer)
        {
            _authHeader = new("Authorization", $"{(isBearer ? "Bearer" : "Bot")} {token}");
            _requestBuckets = new ConcurrentDictionary<Uri, RequestBucket>();
        }

        /// <summary></summary>
        internal Task<HttpResponse> SendRequestAsync(HttpMethod method, Uri route, string payload = null)
        {
            var request = new HttpRequestMessage(method, route);
            request.Headers.Add(_authHeader.Key, _authHeader.Value);

            if (payload is not null) request.Content = JsonContent.Create(payload);

            _requestBuckets.TryGetValue(route, out var bucket);
            bucket ??= RequestBucket.Unlimited;

            if (bucket.TryUse())
                return SendRequest();
            else
                return Task.Run(ScheduleRequest);

            async Task<HttpResponse> SendRequest()
            {
                var response = await _client.SendAsync(request).ConfigureAwait(false);
                using var responsePayload = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var responseJson = await JsonDocument.ParseAsync(responsePayload).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (responseJson.RootElement.GetProperty("global").GetBoolean())
                    {

                    }
                    else
                    {

                    }
                }
                else if (_requestBuckets.TryGetValue(route, out var existingBucket))
                    existingBucket.Update(response.Headers);
                else if (RequestBucket.TryParse(response.Headers, out var newBucket))
                    _requestBuckets.TryAdd(route, newBucket);

                return new HttpResponse()
                {
                    Status = response.StatusCode,
                    Payload = responseJson.RootElement.Clone()
                };
            }

            async Task<HttpResponse> ScheduleRequest()
            {
                await Task.Delay(DateTime.Now - bucket.ResetTime).ConfigureAwait(false);
                var response = await SendRequest().ConfigureAwait(false);

                return response;
            }
        }
    }
}
