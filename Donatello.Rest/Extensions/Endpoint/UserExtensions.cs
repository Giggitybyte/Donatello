namespace Donatello.Rest.Extensions.Endpoint;

using System.Net.Http;
using System.Threading.Tasks;

/// <summary>Default implementations for user endpoints.</summary>
public static class UserExtensions
{
    /// <summary>
    /// <i>For bot tokens:</i> returns the assoicated bot user.<br/>
    /// <i>For bearer tokens:</i> requires OAuth2 <c>identify</c> scope, returns the assoicated Discord user.
    /// </summary>
    public static Task<HttpResponse> GetSelfAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, "users/@me");

    /// <summary>Returns a user object for the provided ID.</summary>
    public static Task<HttpResponse> GetUserAsync(this DiscordHttpClient httpClient, ulong id)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/{id}");

    /// <summary></summary>
    public static Task<HttpResponse> ModifySelfAsync(this DiscordHttpClient httpClient, string payload)
        => httpClient.SendRequestAsync(HttpMethod.Patch, "users/@me", payload);
}
