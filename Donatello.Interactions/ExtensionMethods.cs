namespace Donatello.Interactions;

using System;
using System.Text.Json;
using Donatello.Interactions.Entity;
using Donatello.Interactions.Entity.Channel;

internal static class ExtensionMethods
{
    /// <summary></summary>
    internal static DiscordChannel ToChannel(this JsonElement jsonObject, DiscordBot botInstance)
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

    /// <summary>Converts the JSON token to an array of Discord entities.</summary>
    internal static T[] ToEntityArray<T>(this JsonElement jsonArray, DiscordBot botInstance) where T : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array, got {jsonArray.ValueKind} instead.");

        var array = new T[jsonArray.GetArrayLength()];
        int index = 0;

        foreach (var jsonElement in jsonArray.EnumerateArray())
            array[index++] = jsonElement.ToEntity<T>(botInstance);

        return array;
    }

    /// <summary>Deserializes the JSON token to a string array.</summary>
    internal static string[] ToStringArray(this JsonElement jsonArray)
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array, got {jsonArray.ValueKind} instead.");

        var index = 0;
        var array = new string[jsonArray.GetArrayLength()];

        foreach (var jsonElement in jsonArray.EnumerateArray())
        {
            if (jsonElement.ValueKind is not JsonValueKind.String)
                throw new JsonException($"Expected a string element, got {jsonElement.ValueKind} element instead.");

            array[index++] = jsonElement.GetString();
        }
        
        return array;
    }

    /// <summary>Deserializes the JSON property as string and converts the value to <see langword="ulong"/>.</summary>
    internal static ulong AsUInt64(this JsonElement jsonProperty)
        => jsonProperty.ValueKind is JsonValueKind.String
            ? ulong.Parse(jsonProperty.GetString())
            : throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");
}
