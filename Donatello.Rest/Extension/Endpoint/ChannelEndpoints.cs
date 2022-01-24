namespace Donatello.Rest.Extension.Endpoint;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#get-channel-messages-query-string-params">Click here to see valid query string parameters</see>.</remarks>
    /// <returns>Array of <see href="https://discord.com/developers/docs/resources/channel#message-object">message objects</see>.</returns>
    public static Task<HttpResponse> GetChannelMessagesAsync(this DiscordHttpClient httpClient, ulong channelId, IDictionary<string, string> queryParams = null)
    {
        var urlBuilder = new StringBuilder().Append("channels/").Append(channelId).Append("/messages");

        if (queryParams is not null)
        {
            int index = 0;
            foreach (var param in queryParams)
            {
                if (index++ == 0)
                    urlBuilder.Append('?');
                else
                    urlBuilder.Append('&');

                urlBuilder.Append(param.Key);
                urlBuilder.Append('=');
                urlBuilder.Append(param.Value);
            }
        }

        return httpClient.SendRequestAsync(HttpMethod.Get, urlBuilder.ToString());
    }

    /// <summary>Posts a message to a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#create-message-jsonform-params">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/resources/channel#message-object">message object</see></returns>
    public static Task<HttpResponse> SendMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", json);

    public static Task<HttpResponse> SendMessageAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json, IList<FileAttachment> attachments)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"channels/{channelId}/messages", json, attachments);
}
