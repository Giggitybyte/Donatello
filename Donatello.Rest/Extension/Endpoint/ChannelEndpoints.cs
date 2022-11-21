namespace Donatello.Rest.Extension.Endpoint;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for channel endpoints.</summary>
public static class ChannelEndpoints
{
    /// <summary>Fetches a channel using its ID.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see></returns>
    public static Task<JsonElement> GetChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}").GetJsonAsync();

    /// <summary>Updates the settings for a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#modify-channel">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see>.</returns>
    public static Task<JsonElement> ModifyChannelAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}", jsonDelegate).GetJsonAsync();

    /// <summary>Permananently deletes a guild channel or thread.</summary>
    public static Task<JsonElement> DeleteChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}").GetJsonAsync();

    /// <summary>Fetches a specific message in a channnel.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<JsonElement> GetChannelMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages/{messageId}").GetJsonAsync();

    /// <summary>Fetches up to 100 messages from a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#get-channel-messages-query-string-params">Click here to see valid query parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message objects</see>.</returns>
    public static IAsyncEnumerable<JsonElement> GetChannelMessagesAsync(this DiscordHttpClient httpClient, ulong channelId, params (string key, string value)[] queryParams)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages{queryParams.ToParamString()}").GetJsonArrayAsync();

    /// <summary>Posts a message to a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-message-jsonform-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<JsonElement> CreateMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", jsonDelegate).GetJsonAsync();

    /// <summary>Posts a message to a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-message-jsonform-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<JsonElement> CreateMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> jsonDelegate, IList<File> attachments)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", jsonDelegate, attachments).GetJsonAsync();

    /// <summary>Crosspost a message in a news channel to following channels. </summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<JsonElement> CrosspostMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages/{messageId}/crosspost").GetJsonAsync();

    /// <summary>Creates a reaction for a message in a channel.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<JsonElement> CreateReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me").GetJsonAsync();

    /// <summary>Removes a reaction to a message made by the current user.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<JsonElement> DeleteReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me").GetJsonAsync();

    /// <summary>Removes a reaction to a message made by another user.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<JsonElement> DeleteReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, ulong userId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/{userId}").GetJsonAsync();

    /// <summary>Fetches all users which reacted to the specified message with <paramref name="emoji"/>.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/user#user-object">user objects</see>.</returns>
    public static IAsyncEnumerable<JsonElement> GetReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}").GetJsonArrayAsync();

    /// <summary>Removes reactions for a specific <paramref name="emoji"/> on a message.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<JsonElement> DeleteReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}").GetJsonAsync();

    /// <summary>Removes all reactions on a message.</summary>
    public static Task<JsonElement> DeleteAllReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions").GetJsonAsync();

    /// <summary>Edit a previously sent message.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#edit-message-jsonform-params">Click to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see>.</returns>
    public static Task<JsonElement> ModifyMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}/messages/{messageId}", jsonDelegate).GetJsonAsync();

    /// <summary>Permanently deletes a message.</summary>
    public static Task<JsonElement> DeleteMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}").GetJsonAsync();

    /// <summary>Delete multiple messages in a single request.</summary>
    /// <remarks>This endpoint will not delete messages older than 2 weeks.</remarks>
    public static Task<JsonElement> BulkDeleteMessagesAsync(this DiscordHttpClient httpClient, ulong channelId, IList<ulong> messageIds)
    {
        var requestTask = httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages/bulk-delete", (json) =>
        {
            json.WriteStartArray("messages");

            foreach (var messageId in messageIds)
                json.WriteStringValue(messageId.ToString());

            json.WriteEndArray();
        });

        return requestTask.GetJsonAsync();
    }

    /// <summary>Edit the channel permission overwrites for a user or role in a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#edit-channel-permissions-json-params">Click to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyPermissionOverwriteAsync(this DiscordHttpClient httpClient, ulong channelId, ulong overwriteId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/permissions/{overwriteId}", jsonDelegate);

    /// <summary>Removes a channel permission overwrite for a user or role in a guild channel.</summary>
    public static Task<HttpResponse> DeletePermissionOverwriteAsync(this DiscordHttpClient httpClient, ulong channelId, ulong overwriteId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/permissions/{overwriteId}");

    /// <summary>Fetches all invites for a guild channel.</summary>
    /// <returns>
    /// <see href="https://discord.com/developers/docs/resources/invite#invite-object-invite-structure">Invite objects</see> with
    /// <see href="https://discord.com/developers/docs/resources/invite#invite-metadata-object-invite-metadata-structure">extra invite metadata</see> fields.
    /// </returns>
    public static IAsyncEnumerable<JsonElement> GetChannelInvitesAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/invites").GetJsonArrayAsync();

    /// <summary>Creates a new invite for a guild channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-channel-invite-json-params">Click to see valid JSON parameters</see>.</remarks>
    public static Task<JsonElement> CreateChannelInviteAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/invites", jsonDelegate).GetJsonAsync();

    /// <summary>'Follow' a news channel to send messages to a target channel. </summary>
    /// <remarks>Requires the <c>MANAGE_WEBHOOKS</c> permission in the target channel.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#followed-channel-object">followed channel object</see></returns>
    public static Task<JsonElement> FollowNewsChannelAsync(this DiscordHttpClient httpClient, ulong newsChannelId, ulong targetChannelId)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{newsChannelId}/followers", (json) => json.WriteString("webhook_channel_id", targetChannelId.ToString())).GetJsonAsync();

    /// <summary>Post a typing indicator for the specified channel.</summary>
    public static Task<HttpResponse> TriggerTypingStatusAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/typing");

    /// <summary>Fetches all messages pinned in a channel.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message objects</see>.</returns>
    public static IAsyncEnumerable<JsonElement> GetPinnedMessagesAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/pins").GetJsonArrayAsync();

    /// <summary>Pins a message in a channel.</summary>
    public static Task<HttpResponse> PinMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/pins/{messageId}");

    /// <summary>Unpins a previously message in a channel.</summary>
    public static Task<HttpResponse> UnpinMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/pins/{messageId}");

    /// <summary>Adds a recipient to a Group DM using their access token.</summary>
    public static Task<HttpResponse> AddGroupChannelRecipientAsync(this DiscordHttpClient httpClient, ulong channelId, ulong userId, string accessToken)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/recipients/{userId}", (json) => json.WriteString("access_token", accessToken));

    /// <summary>Removes a recipient from a Group DM.</summary>
    public static Task<HttpResponse> RemoveGroupChannelRecipientAsync(this DiscordHttpClient httpClient, ulong channelId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/recipients/{userId}");

    /// <summary>Creates a new thread from an existing message.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#start-thread-with-message-json-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object-example-thread-channel">thread channel object</see></returns>
    public static Task<JsonElement> CreateThreadChannelAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, Action<Utf8JsonWriter> jsonDelegate)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages/{messageId}", jsonDelegate).GetJsonAsync();

    /// <summary>Creates a new thread that is not connected to an existing message.</summary>
    /// <remarks>Defaults to a private thread. <see href="https://discord.com/developers/docs/resources/channel#start-thread-without-message-json-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#channel-object-example-thread-channel">thread channel object</see></returns>
    public static Task<JsonElement> CreateThreadChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/threads").GetJsonAsync();

    /// <summary>Adds a user to a thread.</summary>
    public static Task<JsonElement> AddThreadChannelMemberAsync(this DiscordHttpClient httpClient, ulong threadChannelId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{threadChannelId}/thread-members/{userId}").GetJsonAsync();

    /// <summary>Adds the current user to a thread.</summary>
    public static Task<JsonElement> AddThreadChannelMemberAsync(this DiscordHttpClient httpClient, ulong threadChannelId)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{threadChannelId}/thread-members/@me").GetJsonAsync();

    /// <summary>Removes a user from a thread.</summary>
    public static Task<JsonElement> RemoveThreadChannelMemberAsync(this DiscordHttpClient httpClient, ulong threadChannelId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{threadChannelId}/thread-members/{userId}").GetJsonAsync();

    /// <summary>Removes the current user from a thread.</summary>
    public static Task<JsonElement> RemoveThreadChannelMemberAsync(this DiscordHttpClient httpClient, ulong threadChannelId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{threadChannelId}/thread-members/@me").GetJsonAsync();

    /// <summary>Fetches a specific member in a thread.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#thread-member-object">thread member object</see></returns>
    public static Task<JsonElement> GetThreadChannelMemberAsync(this DiscordHttpClient httpClient, ulong threadChannelId, ulong userId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{threadChannelId}/thread-members/{userId}").GetJsonAsync();

    /// <summary>Fetches all members in a thread channel.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#thread-member-object">thread member objects</see>.</returns>
    public static IAsyncEnumerable<JsonElement> GetThreadChannelMembersAsync(this DiscordHttpClient httpClient, ulong threadChannelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{threadChannelId}/thread-members").GetJsonArrayAsync();

    /// <summary>Fetches all active threads in a channel.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#list-active-threads-response-body">active threads object</see></returns>
    [Obsolete("Deprecated in Discord API v9; use GetActiveThreadsAsync(ulong guildId) instead.", false)]
    public static Task<JsonElement> GetThreadChannelsAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/threads/active").GetJsonAsync();

    /// <summary>Fetches all archived public thread channels.</summary>
    /// /// <remarks><see href="https://discord.com/developers/docs/resources/channel#list-public-archived-threads-query-string-params">Click to see valid query string params</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#list-public-archived-threads-response-body">archived threads object</see></returns>
    public static Task<JsonElement> GetArchivedPublicThreadsAsync(this DiscordHttpClient httpClient, ulong channelId, params (string key, string value)[] queryParams)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/threads/archived/public{queryParams.ToParamString()}").GetJsonAsync();

    /// <summary>Fetches all archived private thread channels.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#list-private-archived-threads-query-string-params">Click to see valid query string params</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#list-private-archived-threads-response-body">archived threads object</see></returns>
    public static Task<JsonElement> GetArchivedPrivateThreadsAsync(this DiscordHttpClient httpClient, ulong channelId, params (string key, string value)[] queryParams)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/threads/archived/private{queryParams.ToParamString()}").GetJsonAsync();

    /// <summary>Fetches all archived private thread channels that the current user has joined.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#list-joined-private-archived-threads-query-string-params">Click to see valid query string params</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#list-joined-private-archived-threads-response-body">archived threads object</see></returns>
    public static Task<JsonElement> GetJoinedArchivedPrivateThreadsAsync(this DiscordHttpClient httpClient, ulong channelId, params (string key, string value)[] queryParams)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/users/@me/threads/archived/private{queryParams.ToParamString()}").GetJsonAsync();
}