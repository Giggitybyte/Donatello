﻿namespace Donatello.Rest.Extension.Endpoint;

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public static class InviteEndpoints
{
    /// <summary>Fetches a guild invite using its invite code.</summary>
    public static Task<JsonElement> GetInviteAsync(this DiscordHttpClient httpClient, string inviteCode, IDictionary<string,string> queryParams = null)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"invites/{inviteCode}").GetJsonAsync();
}
