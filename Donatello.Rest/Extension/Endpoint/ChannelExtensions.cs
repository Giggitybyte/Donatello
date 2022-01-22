namespace Donatello.Rest.Extension.Endpoint;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public static class ChannelExtensions
{
    /// <summary>Returns a <see href="https://discord.com/developers/docs/resources/channel#channel-object">channel object</see> for the provided ID.</summary>
    public static Task<HttpResponse> GetChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}");

    /// <summary>Updates the settings for a channel.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/resources/channel#modify-channel">Click here to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyChannelAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}", json);

    /// <summary>Permananently deletes a guild channel, thread, or DM channel.</summary>
    public static Task<HttpResponse> DeleteChannelAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"channels/{channelId}");

    /// <summary></summary>
    public static Task<HttpResponse> GetChannelMessageAsync(this DiscordHttpClient httpClient, ulong channelId, ulong messageId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages/{messageId}");

    /// <summary></summary>
    public static Task<HttpResponse> GetChannelMessagesAsync(this DiscordHttpClient httpClient, ulong channelId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"channels/{channelId}/messages");
}
