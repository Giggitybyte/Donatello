namespace Donatello.Rest.Guild;
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
    public static Task<HttpResponse> CreateGuildAsync(this DiscordHttpClient httpClient, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Post, "guilds", json);

    /// <summary>Fetches a guild using its ID.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see></returns>
    public static async Task<JsonElement> GetGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
    {
        var response = await httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}");

        if (response.Status is HttpStatusCode.OK)
            return response.Payload;
        else if (response.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            throw new ArgumentException("Guild ID was invalid", nameof(guildId));
        else
            throw new HttpRequestException($"Unable to fetch guild from Discord: {response.Message} ({(int)response.Status})");
    }

    /// <summary>Fetches the guild preview for a lurkable guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-preview-object">guild preview object</see></returns>
    public static Task<HttpResponse> GetGuildPreviewAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/preview");

    /// <summary>Changes the settings of the guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-object">guild object</see>.</returns>
    public static Task<HttpResponse> ModifyGuildAsync(this DiscordHttpClient httpClient, ulong id, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{id}", json);

    /// <summary>Permanently deletes the guild.</summary>
    /// <remarks>The user associated with the token must be the owner of the guild.</remarks>
    public static Task<HttpResponse> DeleteGuildAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}");

    /// <summary>Fetches all channels in a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel objects</see>.</returns>
    public static Task<HttpResponse> GetGuildChannelsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/channels");

    /// <summary>Creates a new channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-channel-json-params">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see></returns>
    public static Task<HttpResponse> CreateGuildChannelAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/channels", json);

    /// <summary>Changes the position of the provided channel.</summary>
    /// <remarks>Accepts an array; <see href="https://discord.com/developers/docs/resources/guild#modify-guild-channel-positions">click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyChannelPositionAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/channels", json);

    /// <summary>Fetches all active threads in the guild, including private and public threads.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#list-active-threads-response-body">active threads object</see></returns>
    public static Task<HttpResponse> GetActiveThreadsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/threads/active");

    /// <summary>Fetches a guild member for the provided user.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see></returns>
    public static Task<HttpResponse> GetGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/{userId}");

    /// <summary>Fetches all members in a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see>.</returns>
    public static Task<HttpResponse> GetGuildMembersAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members");

    /// <summary>Fetches members with a username or nickname which begins with the provided <paramref name="searchTerm"/>.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member objects</see>.</returns>
    public static Task<HttpResponse> GuildMemberSearchAsync(this DiscordHttpClient httpClient, ulong guildId, string searchTerm, int limit = 500)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/members/search?query={searchTerm}&limit={limit}");

    /// <summary>Grants the specified user access to the guild.</summary>
    /// <remarks>
    /// Requires an OAuth2 access token and a bot user within the guild.
    /// <see href="https://discord.com/developers/docs/resources/guild#add-guild-member">Click to read more and see valid JSON payload parameters</see>.
    /// </remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see></returns>
    public static Task<HttpResponse> AddGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", json);

    /// <summary>Changes attributes of a guild member.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-member">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</returns>
    public static Task<HttpResponse> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}", json);

    /// <summary>Changes attributes of the current member.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-current-member">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/guild#guild-member-object">guild member object</see>.</returns>
    public static Task<HttpResponse> ModifyGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/@me", json);

    /// <summary>Adds a role to a guild member.</summary>
    public static Task<HttpResponse> GrantGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"guilds/{guildId}/members/{userId}/roles/{roleId}");

    /// <summary>Remove a role from a guild member.</summary>
    public static Task<HttpResponse> RevokeGuildMemberRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}/roles/{roleId}");

    /// <summary>Removes a user from a guild.</summary>
    public static Task<HttpResponse> RemoveGuildMemberAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/members/{userId}");

    /// <summary>Fetches all user bans in a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/guild#ban-object">ban objects</see>.</returns>
    public static Task<HttpResponse> GetGuildBansAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans");

    /// <summary>Fetches a ban for the specified user.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/guild#ban-object">ban object</see></returns>
    public static Task<HttpResponse> GetGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/bans/{userId}");

    /// <summary>Permanently bans a user from a guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-ban">Click to see valid JSON payload parameters</see>.</remarks>
    public static Task<HttpResponse> CreateGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId, Action<Utf8JsonWriter> json = null)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"/guilds/{guildId}/bans/{userId}", json);

    /// <summary>Removes a guild ban for a user.</summary>
    public static Task<HttpResponse> DeleteGuildBanAsync(this DiscordHttpClient httpClient, ulong guildId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"/guilds/{guildId}/bans/{userId}");

    /// <summary>Fetches an array of <see href="https://discord.com/developers/docs/topics/permissions#role-object">role objects</see>.</summary>
    public static Task<HttpResponse> GetGuildRolesAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/roles");

    /// <summary>Creates a new role for the guild</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#create-guild-role">Click to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/topics/permissions#role-object">role object</see></returns>
    public static Task<HttpResponse> CreateGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> json = null)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/roles", json);

    /// <summary>Changes the <see href="https://discord.com/developers/docs/topics/permissions#permission-hierarchy">position</see> of the roles provided in your JSON payload.</summary>
    /// <remarks>Accepts an array; <see href="https://discord.com/developers/docs/resources/guild#modify-guild-role-positions">click to see JSON object structure</see>.</remarks>
    public static Task<HttpResponse> ModifyGuildRolePositionAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/roles", json);

    /// <summary>Changes the attributes of a role.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#modify-guild-role">Click to see valid JSON payload parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/topics/permissions#role-object">role object</see></returns>
    public static Task<HttpResponse> ModifyGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong roleId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/roles/{roleId}", json);

    /// <summary>Permanently deletes a role.</summary>
    public static Task<HttpResponse> DeleteGuildRoleAsync(this DiscordHttpClient httpClient, ulong guildId, ulong roleId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/roles/{roleId}");

    /// <summary>Returns an object with one 'pruned' key indicating the number of members that would be removed in a prune operation.</summary>
    /// <param name="inactiveDays">Member inactivity threshold.</param>
    /// <param name="includedRoles">
    /// By default, a prune operation will not remove users with roles.<br/> 
    /// Any inactive user that has a subset of the provided role(s) will be counted in the prune and users with additional roles will not.
    /// </param>
    public static Task<HttpResponse> GetGuildPruneCountAsync(this DiscordHttpClient httpClient, ulong guildId, int inactiveDays = 7, IEnumerable<ulong> includedRoles = null)
    {
        includedRoles ??= Array.Empty<ulong>();
        var roles = string.Join(',', includedRoles);

        return httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/prune?days={inactiveDays}&includeRoles={roles}");
    }

    /// <summary>Removes inactive guild members.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/guild#begin-guild-prune">Click to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> PruneGuildMembersAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/prune", jsonWriter);

    /// <summary>Fetches available voice regions for a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/voice#voice-region-object">voice region objects</see>.</returns>
    public static Task<HttpResponse> GetGuildVoiceRegionsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/regions");

    /// <summary>Fetches all invites for a guild.</summary>
    /// <returns>
    /// Array of <see href="https://discord.com/developers/docs/resources/invite#invite-object-invite-structure">invite objects</see>.
    /// Objects will include <see href="https://discord.com/developers/docs/resources/invite#invite-metadata-object-invite-metadata-structure">extra invite metadata</see>.
    /// </returns>
    public static Task<HttpResponse> GetGuildInvitesAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/invites");

    /// <summary>Fetches all integrations for a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/guild#integration-object">integration objects</see>.</returns>
    public static Task<HttpResponse> GetGuildIntegrationsAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/integrations");

    /// <summary>Removes an integration from a guild.</summary>
    public static Task<HttpResponse> DeleteGuildIntegrationAsync(this DiscordHttpClient httpClient, ulong guildId, JsonElement integrationObject)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/integrations", integrationObject);

    /// <summary>Fetches a guild vanity invite.</summary>
    /// <returns>Partial <see href="https://discord.com/developers/docs/resources/invite#invite-object">invite object</see>.</returns>
    public static Task<HttpResponse> GetGuildVanityUrlAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/vanity-url");

    /// <summary>Fetches the audit log for a guild.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/audit-log#audit-log-object"> audit log object</see></returns>
    public static Task<HttpResponse> GetGuildAuditLogAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/audit-logs");

    /// <summary>Fetches all emojis from a guild.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji objects</see>.</returns>
    public static Task<HttpResponse> GetGuildEmojisAsync(this DiscordHttpClient httpClient, ulong guildId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/emojis");

    /// <summary>Fetches a specific guild emoji.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see></returns>
    public static Task<HttpResponse> GetGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"guilds/{guildId}/emojis/{emojiId}");

    /// <summary>Adds a new emoji to the guild.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/emoji#create-guild-emoji">Click to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see></returns>
    public static Task<HttpResponse> CreateGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"guilds/{guildId}/emojis", jsonWriter);

    /// <summary>Changes attributes of an emoji.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/emoji#modify-guild-emoji">Click to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/emoji#emoji-object">emoji object</see>.</returns>
    public static Task<HttpResponse> ModifyGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"guilds/{guildId}/emojis/{emojiId}");

    /// <summary>Permanently deletes a guild emoji.</summary>
    public static Task<HttpResponse> DeleteGuildEmojiAsync(this DiscordHttpClient httpClient, ulong guildId, ulong emojiId)
    => httpClient.SendRequestAsync(HttpMethod.Delete, $"guilds/{guildId}/emojis/{emojiId}");
}
