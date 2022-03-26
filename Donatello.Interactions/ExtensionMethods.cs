namespace Donatello.Interactions;

using System;
using System.Text.Json;
using Donatello.Interactions.Entity;

internal static class ExtensionMethods
{
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

    /// <summary>Converts a JSON token to an array of Discord entities.</summary>
    internal static T[] ToEntityArray<T>(this JsonElement jsonArray, DiscordBot botInstance) where T : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array, got {jsonArray.ValueKind} instead.");

        var array = new T[jsonArray.GetArrayLength()];
        int index = 0;

        foreach (var jsonElement in jsonArray.EnumerateArray())
        {
            T entity = typeof(T) == typeof(DiscordChannel)
                ? jsonElement.ToChannelEntity(botInstance) as T
                : Activator.CreateInstance(typeof(T), botInstance, jsonElement) as T;

            array[index++] = entity;
        }

        return array;
    }
}
