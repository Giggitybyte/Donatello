namespace Donatello.Gateway;

using Donatello.Core.Rest;
using System;
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

internal static class ExtensionMethods
{
    /// <summary>Fetches up-to-date gateway connection information from the Discord REST API.</summary>
    internal static async Task<JsonElement> GetGatewayMetadataAsync(this DiscordHttpClient httpClient)
    {
        var response = await httpClient.SendRequestAsync(HttpMethod.Get, $"gateway/bot");

        if (response.Status is not HttpStatusCode.OK)
            throw new HttpRequestException("Could not retreive shard metadata.", null, response.Status);

        return response.Payload;
    }

    /// <summary>
    /// Preforms a pseudo-resize of a rented array.
    /// </summary>
    internal static void Resize<T>(this ArrayPool<T> pool, ref T[] array, int newSize)
    {
        T[] newArray = pool.Rent(newSize);
        int itemsToCopy = Math.Min(array.Length, newSize);

        Array.Copy(array, 0, newArray, 0, itemsToCopy);
        pool.Return(array, true);

        array = newArray;
    }
}
