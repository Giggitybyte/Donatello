namespace Donatello.Rest.Endpoint;

using Donatello.Rest.Transport;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public static class InviteEndpoints
{
    /// <summary>Fetches a guild invite using its invite code.</summary>
    public static Task<HttpResponse> GetInviteAsync(this DiscordHttpClient httpClient, string inviteCode, IDictionary<string,string> queryParams = null)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"invites/{inviteCode}");
}
