using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Rest.Ratelimit;

namespace Donatello.Rest.Endpoints
{
    public abstract class ApiEndpoint
    {
        private readonly DiscordApi _apiClient;
        private ConcurrentDictionary<ulong, RequestBucket> _requestBuckets;

        protected ApiEndpoint(DiscordApi httpClient)
        {
            _apiClient = httpClient;
        }

        protected ValueTask<JsonElement> SendAsync(HttpMethod method, )
        {

        }
    }
}
