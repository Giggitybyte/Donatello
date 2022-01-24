namespace Donatello.Rest.Endpoint;

using Donatello.Rest.Transport;
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
    public static Task<HttpResponse> GetChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}");

    /// <summary>Updates the settings for a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#modify-channel">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see>.</returns>
    public static Task<HttpResponse> ModifyChannelAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}", json);

    /// <summary>Permananently deletes a guild channel or thread.</summary>
    public static Task<HttpResponse> DeleteChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}");

    /// <summary>Fetches a specific message in a channnel.</summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<HttpResponse> GetChannelMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages/{messageId}");

    /// <summary>Fetches up to 100 messages from a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#get-channel-messages-query-string-params">Click here to see valid query parameters</see>.</remarks>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/channel#message-object">message objects</see>.</returns>
    public static Task<HttpResponse> GetChannelMessagesAsync(this DiscordHttpClient httpClient, ulong channelId, IDictionary<string, string> queryParams = null)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages{queryParams.ToParamString()}");

    /// <summary>Posts a message to a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-message-jsonform-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<HttpResponse> PostMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", json);

    /// <summary>Posts a message to a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-message-jsonform-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<HttpResponse> PostMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json, IList<FileAttachment> attachments)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", json, attachments);

    /// <summary>Crosspost a message in a news channel to following channels. </summary>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<HttpResponse> CrosspostMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages/{messageId}/crosspost");

    /// <summary>Creates a reaction for a message in a channel.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<HttpResponse> CreateReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me");

    /// <summary>Removes a reaction to a message made by the current user.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<HttpResponse> DeleteReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me");

    /// <summary>Removes a reaction to a message made by another user.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<HttpResponse> DeleteReactionAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, ulong userId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/{userId}");

    /// <summary>Fetches all users which reacted with <paramref name="emoji"/>.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/user#user-object">user objects</see>.</returns>
    public static Task<HttpResponse> GetReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}");

    /// <summary>Removes reactions for a specific <paramref name="emoji"/> on a message.</summary>
    /// <remarks>
    /// <paramref name="emoji"/> must be <see href="https://en.wikipedia.org/wiki/Percent-encoding">URL encoded</see> when using unicode emoji.<br/>
    /// To use custom emoji, you must encode <paramref name="emoji"/> in the format <b><c>name:id</c></b> with the emoji name and emoji id.
    /// </remarks>
    public static Task<HttpResponse> DeleteReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, string emoji)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions/{emoji}");

    /// <summary>Removes all reactions on a message.</summary>
    public static Task<HttpResponse> DeleteAllReactionsAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}/reactions");

    /// <summary>Edit a previously sent message.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#edit-message-jsonform-params">Click to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see>.</returns>
    public static Task<HttpResponse> ModifyMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}/messages/{messageId}", json);

    /// <summary>Permanently deletes a message.</summary>
    public static Task<HttpResponse> DeleteMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}/messages/{messageId}");

    /// <summary>Delete multiple messages in a single request.</summary>
    /// <remarks>This endpoint will not delete messages older than 2 weeks.</remarks>
    public static Task<HttpResponse> BulkDeleteMessagesAsync(this DiscordHttpClient httpClient, ulong channelId, IList<ulong> messageIds)
    {
        return httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages/bulk-delete", json =>
        {
            json.WriteStartArray("messages");

            foreach (var messageId in messageIds)
                json.WriteStringValue(messageId.ToString());

            json.WriteEndArray();
        });
    }

    /// <summary>Edit the channel permission overwrites for a user or role in a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#edit-channel-permissions-json-params">Click to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyPermissionOverwriteAsync(this DiscordHttpClient httpClient, ulong channelId, ulong overwriteId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"channels/{channelId}/permissions/{overwriteId}", json);

    /// <summary>Fetches all invites for a guild channel.</summary>
    /// <returns>
    /// Array of <see href="https://discord.com/developers/docs/resources/invite#invite-object-invite-structure">invite objects</see>.
    /// Objects will include <see href="https://discord.com/developers/docs/resources/invite#invite-metadata-object-invite-metadata-structure">extra invite metadata</see>.
    /// </returns>
    public static Task<HttpResponse> GetChannelInvitesAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/invites");

    /// <summary>Creates a new invite for a guild channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-channel-invite-json-params">Click to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> CreateChannelInviteAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/invites", json);
}