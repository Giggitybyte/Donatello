namespace Donatello.Interactions;

using System;
using System.Text.Json;
using Donatello.Interactions.Entity;

internal static class ExtensionMethods
{

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
