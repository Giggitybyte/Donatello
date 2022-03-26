namespace Donatello.Rest.Endpoint;

using Donatello.Rest.Transport;
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
    public static Task<HttpResponse> GetSelfAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, "users/@me");

    /// <summary>Fetches a user using its ID.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/user#user-object">user object</see></returns>
    public static async Task<JsonElement> GetUserAsync(this DiscordHttpClient httpClient, ulong userId)
    {
        var response = await httpClient.SendRequestAsync(HttpMethod.Get, $"users/{userId}");

        if (response.Status is HttpStatusCode.OK)
            return response.Payload;
        else if (response.Status is HttpStatusCode.NotFound)
            throw new ArgumentException("User ID was invalid", nameof(userId));
        else
            throw new HttpRequestException($"Unable to fetch user from Discord: {response.Message} ({(int)response.Status})");
    }

    /// <summary>Changes user account settings.</summary>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/user#user-object">user object</see>.</returns>
    public static Task<HttpResponse> ModifySelfAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Patch, "users/@me", jsonWriter);

    /// <summary>Fetchs all guilds that the current user is a member of.</summary>
    /// <remarks>Requires the OAuth2 <c>guilds</c> scope when using a bearer token.</remarks>
    /// <returns>Array of partial <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild objects</see>.</returns>
    public static Task<HttpResponse> GetGuildsAsync(this DiscordHttpClient httpClient)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"users/@me/guilds");

    /// <summary>Removes the user from a guild.</summary>
    public static Task<HttpResponse> LeaveGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"users/@me/guilds/{guildId}");
}
