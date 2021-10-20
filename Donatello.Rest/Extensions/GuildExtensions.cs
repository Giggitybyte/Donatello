namespace Donatello.Rest.Extensions;

using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class GuildExtensions
{
    public static Task<HttpResponse> GetGuildAsync(this DiscordHttpClient httpClient, ulong id)
    {
        var endpoint = new Uri($"/guilds/{id}");
        return httpClient.SendRequestAsync(HttpMethod.Get, endpoint);
    }

    public static async Task<HttpResponse> ModifyGuildAsync(this DiscordHttpClient httpClient, ulong id, )
}
