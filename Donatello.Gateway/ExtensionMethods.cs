namespace Donatello.Gateway;

using Donatello.Gateway.Entity;
using Donatello.Rest;
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

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static DiscordChannel ToChannelEntity(this JsonElement jsonObject, DiscordBot botInstance)
    {
        var type = jsonObject.GetProperty("type").GetInt32();

        DiscordChannel channel = type switch
        {
            0 => new DiscordGuildTextChannel(botInstance, jsonObject),
            1 => new DiscordDirectTextChannel(botInstance, jsonObject),
            2 or 13 => new DiscordVoiceChannel(botInstance, jsonObject),
            3 => throw new NotSupportedException("Bot accounts cannot be in group DMs."),
            4 => new DiscordCategoryChannel(botInstance, jsonObject),
            5 => new DiscordAnnouncementChannel(botInstance, jsonObject),
            10 or 11 or 12 => new DiscordThreadTextChannel(botInstance, jsonObject),
            _ => throw new JsonException("Unknown channel type.")
        };

        return channel;
    }

    /// <summary>Deserializes the JSON property as string and converts the value to <see langword="ulong"/>.</summary>
    internal static ulong AsUInt64(this JsonElement jsonProperty) // TODO: proper snowflake type.
        => jsonProperty.ValueKind is JsonValueKind.String
            ? ulong.Parse(jsonProperty.GetString())
            : throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");
}
