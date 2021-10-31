namespace Donatello.Rest.Extensions.Endpoint;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for guild endpoints.</summary>
public static class GuildExtensions
{
    /// <summary>Returns the newly created <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> CreateGuildAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds", jsonBuilder);

    /// <summary>Returns a <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see> for the provided ID.</summary>
    public static Task<HttpResponse> GetGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}");

    /// <summary>Returns a <see href="https://discord.com/developers/docs/resources/guild#guild-preview-object">guild preview object</see> for the guild.</summary>
    public static Task<HttpResponse> GetGuildPreviewAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/preview");

    /// <summary>Changes the settings for the guild; returns an updated <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyGuildAsync(this DiscordHttpClient httpClient, ulong id, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{id}", jsonBuilder);

    /// <summary>Permanently deletes the guild.</summary>
    /// <remarks>The user associated with the token must be the owner of the guild.</remarks>
    public static Task<HttpResponse> DeleteGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}");

    /// <summary>Returns an array of <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel objects</see> in the guild.</summary>
    public static Task<HttpResponse> GetGuildChannelsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/channels");

    /// <summary>Returns the newly created <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-channel-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> CreateGuildChannelAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/channels", jsonBuilder);

    /// <summary>Changes the position of the provided channel.</summary>
    /// <remarks>Accepts an array; <see href="https://discord.com/developers/docs/resources/guild#modify-guild-channel-positions">click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyChannelPositionAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/channels", jsonBuilder);

    /// <summary>Fetches all active threads in the guild, including private and public threads.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#list-active-threads">Click to see the fields of the returned object</see>.</remarks>
    public static Task<HttpResponse> GetActiveThreadsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/threads/active");

    /// <summary>Fetches a <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see> for the provided user.</summary>
    public static Task<HttpResponse> GetGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/{userId}");

    /// <summary>Returns an array of <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see>.</summary>
    public static Task<HttpResponse> GetGuildMembersAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members");

    /// <summary>
    /// Fetches an array of <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see> 
    /// for members with a username or nickname starts with the provided <paramref name="searchTerm"/>.
    /// </summary>
    public static Task<HttpResponse> GuildMemberSearchAsync(this DiscordHttpClient httpClient, ulong guildId, string searchTerm, int limit = 500)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/search?query={searchTerm}&limit={limit}");

    /// <summary>
    /// Grants the specified user access to the guild and returns a newly created 
    /// <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member</see>.
    /// </summary>
    /// <remarks>
    /// Requires an OAuth2 access token and a bot user within the guild.
    /// <see href="https://discord.com/developers/docs/resources/guild#add-guild-member">Click to see valid JSON payload parameters</see>.
    /// </remarks>
    public static Task<HttpResponse> AddGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", jsonBuilder);

    /// <summary>Changes attributes of a guild member; returns an updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-member">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", jsonBuilder);

    /// <summary>Changes attributes of the current member; returns an updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-current-member">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/@me", jsonBuilder);

    /// <summary>Adds a role to a guild member.</summary>
    public static Task<HttpResponse> GrantGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}/roles/{roleId}");

    /// <summary>Remove a role from a guild member.</summary>
    public static Task<HttpResponse> RevokeGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}/roles/{roleId}");

    /// <summary>Removes a user from a guild.</summary>
    public static Task<HttpResponse> RemoveGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}");

    /// <summary>Fetches an array of <see href="https://discord.com/developers/docs/resources/guild#ban-object">ban objects</see>.</summary>
    public static Task<HttpResponse> GetGuildBansAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans");

    /// <summary>Fetches a guild <see href="https://discord.com/developers/docs/resources/guild#ban-object">ban</see> for the specified user.</summary>
    public static Task<HttpResponse> GetGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans/{userId}");

    /// <summary>Permanently bans a user from a guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-ban">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> CreateGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonBuilder = null)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"/guilds/{guildId}/bans/{userId}", jsonBuilder);

    /// <summary>Removes the guild ban for a user.</summary>
    public static Task<HttpResponse> RemoveGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"/guilds/{guildId}/bans/{userId}");

    /// <summary>Fetches an array of <see href="https://discord.com/developers/docs/topics/permissions#role-object">role objects</see>.</summary>
    public static Task<HttpResponse> GetGuildRolesAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/roles");
}
