namespace Donatello.Rest.Extension.Endpoint;

using Donatello.Rest.Extension.Internal;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for user endpoints.</summary>
public static class UserEndpoints
{
    /// <summary><i>For bot tokens:</i> returns the assoicated bot user.<br/><i>For bearer tokens:</i> returns the assoicated Discord user.<br/></summary>
    /// <remarks>Using a bearer token this requires the <c>identify</c> scope, which will return the object without an email, and optionally the <c>email</c> scope, which returns the object with an email.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/user#user-object">user object</see></returns>
    public static Task<JsonElement> GetSelfAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, "users/@me").GetJsonAsync();

    /// <summary>Fetches a user using its ID.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/user#user-object">user object</see></returns>
    public static Task<JsonElement> GetUserAsync(this DiscordHttpClient httpClient, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/{userId}").GetJsonAsync();

    /// <summary>Changes user account settings.</summary>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/user#user-object">user object</see>.</returns>
    public static Task<JsonElement> ModifySelfAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, "users/@me", jsonDelegate).GetJsonAsync();

    /// <summary>Fetchs all guilds that the current user is a member of.</summary>
    /// <remarks>Requires the OAuth2 <c>guilds</c> scope when using a bearer token.</remarks>
    /// <returns>Array of partial <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild objects</see>.</returns>
    public static Task<JsonElement> GetGuildsAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/@me/guilds").GetJsonAsync();

    /// <summary>Removes the user from a guild.</summary>
    public static Task<JsonElement> LeaveGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"users/@me/guilds/{guildId}").GetJsonAsync();
}
