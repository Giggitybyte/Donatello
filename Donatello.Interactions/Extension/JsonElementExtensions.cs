﻿namespace Donatello.Interactions.Extension;

using System;
using System.Text.Json;
using Donatello.Interactions.Entity;

internal static class JsonElementExtensions
{
    /// <summary></summary>
    internal static T ToEntity<T>(this JsonElement jsonObject, DiscordBot botInstance) where T : DiscordEntity
    {
        if (typeof(T) == typeof(DiscordChannel))
        {
            var type = jsonObject.GetProperty("type").GetInt32();

            DiscordChannel channel = type switch
            {
                0 => new DiscordGuildTextChannel(botInstance, jsonObject),
                1 => new DiscordDirectTextChannel(botInstance, jsonObject),
                2 or 13 => new DiscordVoiceChannel(botInstance, jsonObject),
                3 => throw new NotSupportedException("Bot accounts cannot be in group DMs."),
                4 => new DiscordCategoryChannel(botInstance, jsonObject),
                5 => new DiscordAnn
                10 or 11 or 12 => new DiscordThreadTextChannel(botInstance, jsonObject),
                _ => throw new JsonException("Unknown channel type.")
            };

            return channel as T;
        }
        else
            return Activator.CreateInstance(typeof(T), botInstance, jsonObject) as T;
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
    {
        if (jsonProperty.ValueKind is not JsonValueKind.String)
            throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");

        return ulong.Parse(jsonProperty.GetString());
    }
}
