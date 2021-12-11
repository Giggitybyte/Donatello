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

    /// <summary></summary>
    public static Task<HttpResponse> ModifyChannelAsync(this DiscordHttpClient httpClient, ulong channelId, Action<Utf8JsonWriter> json)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"channels/{channelId}", json);
}
