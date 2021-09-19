namespace Donatello.Rest.Routes;

using System;
using System.Text.Json;
using System.Threading.Tasks;

public class GuildRoute : ApiRoute
{
    internal GuildRoute(DiscordHttpClient apiClient) : base(apiClient) { }

    /// <summary>Returns the guild object for the given id.</summary>
    public ValueTask<HttpResponse> GetAsync(ulong id)
    {
        throw new NotImplementedException();
    }

    public ValueTask<HttpResponse> ModifyAsync(ulong id, JsonElement modifyJson)
    {
        throw new NotImplementedException();
    }

    public ValueTask<HttpResponse> GetChannelsAsync()
    {
        throw new NotImplementedException();
    }
}
