namespace Donatello.Rest.Extension.Endpoint;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for guild endpoints.</summary>
public static class GuildEndpoints
{
    /// <summary>Creates a new guild owned by the current user.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see></returns>
    public static Task<JsonElement> CreateGuildAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, "guilds", jsonDelegate).GetJsonAsync();

    /// <summary>Fetches a guild using its ID.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see></returns>
    public static async Task<JsonElement> GetGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
    {
        var response = await httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}");

        if (response.Status is HttpStatusCode.OK)
            return response.Payload;
        else if (response.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            throw new ArgumentException(response.Payload.GetProperty("message").GetString(), nameof(guildId));
        else
            throw new HttpRequestException($"Unable to fetch guild from Discord: {response.Message} ({(int)response.Status})");
    }

    /// <summary>Fetches the guild preview for a lurkable guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-preview-object">guild preview object</see></returns>
    public static Task<JsonElement> GetGuildPreviewAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/preview").GetJsonAsync();

    /// <summary>Changes the settings of the guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see>.</returns>
    public static Task<JsonElement> ModifyGuildAsync(this DiscordHttpClient httpClient, ulong id, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{id}", jsonDelegate).GetJsonAsync();

    /// <summary>Permanently deletes the guild.</summary>
    /// <remarks>The user associated with the token must be the owner of the guild.</remarks>
    public static Task<JsonElement> DeleteGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}").GetJsonAsync();

    /// <summary>Fetches all channels in a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object">channel objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildChannelsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/channels").GetJsonArrayAsync();

    /// <summary>Creates a new channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-channel-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see></returns>
    public static Task<JsonElement> CreateGuildChannelAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/channels", jsonDelegate).GetJsonAsync();

    /// <summary>Changes the position of the provided channel.</summary>
    /// <remarks>Accepts an array; <see href="https://discord.com/developers/docs/resources/guild#modify-guild-channel-positions">click to see valid JSON payload parameters</see>.</remarks>
    public static Task<JsonElement> ModifyChannelPositionsAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/channels", jsonDelegate).GetJsonAsync();

    /// <summary>Fetches all active threads in the guild, including private and public threads.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#list-active-guild-threads-response-body">active threads object</see></returns>
    public static Task<JsonElement> GetActiveThreadsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/threads/active").GetJsonAsync();

    /// <summary>Fetches a guild member for the provided user.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see></returns>
    public static Task<JsonElement> GetGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/{userId}").GetJsonAsync();

    /// <summary>Fetches all members in a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildMembersAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members").GetJsonArrayAsync();

    /// <summary>Fetches members with a username or nickname which begins with the provided <paramref name="searchTerm"/>.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GuildMemberSearchAsync(this DiscordHttpClient httpClient, ulong guildId, string searchTerm, int limit = 500)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/search?query={searchTerm}&limit={limit}").GetJsonArrayAsync();

    /// <summary>Grants the specified user access to the guild.</summary>
    /// <remarks>
    /// Requires an OAuth2 access token and a bot user within the guild.
    /// <see href="https://discord.com/developers/docs/resources/guild#add-guild-member">Click to read more and see valid JSON payload parameters</see>.
    /// </remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see></returns>
    public static Task<JsonElement> AddGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", jsonDelegate).GetJsonAsync();

    /// <summary>Changes attributes of a guild member.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-member">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</returns>
    public static Task<JsonElement> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", jsonDelegate).GetJsonAsync();

    /// <summary>Changes attributes of the current member.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-current-member">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</returns>
    public static Task<JsonElement> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/@me", jsonDelegate).GetJsonAsync();

    /// <summary>Adds a role to a guild member.</summary>
    public static Task<JsonElement> GrantGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}/roles/{roleId}").GetJsonAsync();

    /// <summary>Remove a role from a guild member.</summary>
    public static Task<JsonElement> RevokeGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}/roles/{roleId}").GetJsonAsync();

    /// <summary>Removes a user from a guild.</summary>
    public static Task<JsonElement> RemoveGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}").GetJsonAsync();

    /// <summary>Fetches all user bans in a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#ban-object">ban objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildBansAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans").GetJsonArrayAsync();

    /// <summary>Fetches a ban for the specified user.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#ban-object">ban object</see></returns>
    public static Task<JsonElement> GetGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans/{userId}").GetJsonAsync();

    /// <summary>Permanently bans a user from a guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-ban">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<JsonElement> CreateGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> jsonDelegate = null)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"/guilds/{guildId}/bans/{userId}", jsonDelegate).GetJsonAsync();

    /// <summary>Removes a guild ban for a user.</summary>
    public static Task<JsonElement> DeleteGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"/guilds/{guildId}/bans/{userId}").GetJsonAsync();

    /// <returns><see href="https://discord.com/developers/docs/topics/permissions#role-object">role objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildRolesAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/roles").GetJsonArrayAsync();

    /// <summary>Creates a new role for the guild</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-role">Click to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/topics/permissions#role-object">role object</see></returns>
    public static Task<JsonElement> CreateGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate = null)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/roles", jsonDelegate).GetJsonAsync();

    /// <summary>Changes the <see href="https://discord.com/developers/docs/topics/permissions#permission-hierarchy">position</see> of the roles provided in your JSON payload.</summary>
    /// <remarks>Accepts an array; <see href="https://discord.com/developers/docs/resources/guild#modify-guild-role-positions">click to see JSON object structure</see>.</remarks>
    public static Task<JsonElement> ModifyGuildRolePositionAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/roles", jsonDelegate).GetJsonAsync();

    /// <summary>Changes the attributes of a role.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-role">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/topics/permissions#role-object">role object</see></returns>
    public static Task<JsonElement> ModifyGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong roleId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/roles/{roleId}", jsonDelegate).GetJsonAsync();

    /// <summary>Permanently deletes a role.</summary>
    public static Task<JsonElement> DeleteGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/roles/{roleId}").GetJsonAsync();

    /// <summary>Returns an object with one 'pruned' key indicating the number of members that would be removed in a prune operation.</summary>
    /// <param name="inactiveDays">Member inactivity threshold.</param>
    /// <param name="includedRoles">
    /// By default, a prune operation will not remove users with roles.<br/> 
    /// Any inactive user that has a subset of the provided role(s) will be counted in the prune and users with additional roles will not.
    /// </param>
    public static Task<JsonElement> GetGuildPruneCountAsync(this DiscordHttpClient httpClient, ulong guildId, int inactiveDays = 7, IEnumerable<ulong> includedRoles = null)
    {
        includedRoles ??= Array.Empty<ulong>();
        var roles = string.Join(',', includedRoles);

        return httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/prune?days={inactiveDays}&includeRoles={roles}").GetJsonAsync();
    }

    /// <summary>Removes inactive guild members.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#begin-guild-prune">Click to see valid JSON parameters</see>.</remarks>
    public static Task<JsonElement> PruneGuildMembersAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/prune", jsonDelegate).GetJsonAsync();

    /// <summary>Fetches available voice regions for a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/voice#voice-region-object">voice region objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildVoiceRegionsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/regions").GetJsonArrayAsync();

    /// <summary>Fetches all invites for a guild.</summary>
    /// <returns>
    /// <see href="https://discord.com/developers/docs/resources/invite#invite-object-invite-structure">invite objects</see> with
    /// <see href="https://discord.com/developers/docs/resources/invite#invite-metadata-object-invite-metadata-structure">extra invite metadata</see>.
    /// </returns>
    public static IAsyncEnumerable<JsonElement> GetGuildInvitesAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/invites").GetJsonArrayAsync();

    /// <summary>Fetches all integrations for a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#integration-object">integration objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildIntegrationsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/integrations").GetJsonArrayAsync();

    /// <summary>Removes an integration from a guild.</summary>
    public static Task<JsonElement> DeleteGuildIntegrationAsync(this DiscordHttpClient httpClient, ulong guildId, JsonElement integrationObject)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/integrations", integrationObject).GetJsonAsync();

    /// <summary>Fetches a guild vanity invite.</summary>
    /// <returns>Partial <see href="https://discord.com/developers/docs/resources/invite#invite-object">invite object</see>.</returns>
    public static Task<JsonElement> GetGuildVanityUrlAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/vanity-url").GetJsonAsync();

    /// <summary>Fetches the audit log for a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/audit-log#audit-log-object"> audit log object</see></returns>
    public static Task<JsonElement> GetGuildAuditLogAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/audit-logs").GetJsonAsync();

    /// <summary>Fetches all emojis from a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji objects</see></returns>
    public static IAsyncEnumerable<JsonElement> GetGuildEmojisAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/emojis").GetJsonArrayAsync();

    /// <summary>Fetches a specific guild emoji.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see></returns>
    public static Task<JsonElement> GetGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/emojis/{emojiId}").GetJsonAsync();

    /// <summary>Adds a new emoji to the guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/emoji#create-guild-emoji">Click to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see></returns>
    public static Task<JsonElement> CreateGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/emojis", jsonDelegate).GetJsonAsync();

    /// <summary>Changes attributes of an emoji.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/emoji#modify-guild-emoji">Click to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see>.</returns>
    public static Task<JsonElement> ModifyGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/emojis/{emojiId}").GetJsonAsync();

    /// <summary>Permanently deletes a guild emoji.</summary>
    public static Task<JsonElement> DeleteGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/emojis/{emojiId}").GetJsonAsync();
}
