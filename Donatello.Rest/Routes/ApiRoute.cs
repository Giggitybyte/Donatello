namespace Donatello.Rest.Routes;

using System;
using System.Net.Http;
using System.Threading.Tasks;

public abstract class ApiRoute
{
    private DiscordHttpClient _apiClient;

    internal ApiRoute(DiscordHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    protected ValueTask<HttpResponse> SendRequestAsync(HttpMethod method, Uri route, string payload = null)
        => _apiClient.SendRequestAsync(method, route, payload);
}
