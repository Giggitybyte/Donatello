namespace Donatello.Rest.Extension.Endpoint;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for user endpoints.</summary>
public static class UserExtensions
{

    /// <summary>Returns a <see href="https://discord.com/developers/docs/resources/user#get-current-user">user object</see> for the requester's account.</summary>
    /// <remarks>
    /// <i>For bot tokens:</i> returns the assoicated bot user.<br/>
    /// <i>For bearer tokens:</i> returns the assoicated Discord user.<br/>
    /// For OAuth2: this requires the <c>identify</c> scope, which will return the object without an email, and optionally the <c>email</c> scope, which returns the object with an email.
    /// </remarks>
    public static Task<HttpResponse> GetSelfAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, "users/@me");

    /// <summary>Returns a <see href="https://discord.com/developers/docs/resources/user#get-current-user">user object</see> for the provided ID.</summary>
    public static Task<HttpResponse> GetUserAsync(this DiscordHttpClient httpClient, ulong id)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/{id}");

    /// <summary>Changes user account settings. Returns an updated <see href="https://discord.com/developers/docs/resources/user#get-current-user">user object</see>.</summary>
    public static Task<HttpResponse> ModifySelfAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Patch, "users/@me", jsonBuilder);

    /// <summary>Returns an array of partial <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild objects</see> that the user is a member of.</summary>
    /// <remarks>Requires the <c>guilds</c> OAuth2 scope.</remarks>
    public static Task<HttpResponse> GetGuildsAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/@me/guilds");

    /// <summary>Removes the user from a guild.</summary>
    public static Task<HttpResponse> LeaveGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"users/@me/guilds/{guildId}");
}
