namespace Donatello.Extension.Internal;

using Donatello.Entity;
using System;
using System.Collections.Generic;
using System.Text.Json;

internal static class InternalExtensionMethods
{
    /// <summary>Converts a JSON array to a native array of strings.</summary>
    internal static string[] ToStringArray(this JsonElement jsonArray)
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

        var array = new string[jsonArray.GetArrayLength()];
        int index = 0;

        foreach (var jsonElement in jsonArray.EnumerateArray())
            array[index++] = jsonElement.GetString();

        return array;
    }

    /// <summary>Deserializes the JSON property as string and converts the value to a <see cref="DiscordSnowflake"/>.</summary>
    internal static DiscordSnowflake ToSnowflake(this JsonElement jsonProperty)
    {
        if (jsonProperty.ValueKind is not JsonValueKind.String)
            throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");

        return ulong.Parse(jsonProperty.GetString());
    }

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static DiscordChannel ToChannelEntity(this JsonElement jsonObject, DiscordApiBot botInstance)
    {
        var type = jsonObject.GetProperty("type").GetInt32();

        DiscordChannel channel = type switch
        {
            0 => new DiscordGuildTextChannel(botInstance, jsonObject),
            1 => new DiscordDirectTextChannel(botInstance, jsonObject),
            2 => new DiscordVoiceChannel(botInstance, jsonObject),
            3 => throw new NotSupportedException("Bot accounts cannot be in group DMs."),
            4 => new DiscordCategoryChannel(botInstance, jsonObject),
            5 => new DiscordAnnouncementChannel(botInstance, jsonObject),
            10 or 11 or 12 => new DiscordThreadTextChannel(botInstance, jsonObject),
            13 => new DiscordStageChannel(botInstance, jsonObject),
            14 => new DiscordDirectoryChannel(botInstance, jsonObject),
            15 => DiscordForumChannel(botInstance, jsonObject),
            _ => throw new JsonException("Unknown channel type.")
        };

        return channel;
    }

    /// <summary>
    /// Converts each element in a JSON array to a <typeparamref name="TEntity"/> and returns 
    /// the collection as a dictionary, where the key is the snowflake ID of each entity value.
    /// </summary>
    internal static Dictionary<DiscordSnowflake, TEntity> ToEntityDictionary<TEntity>(this JsonElement jsonArray, DiscordApiBot botInstance) where TEntity : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

        var dictionary = new Dictionary<DiscordSnowflake, TEntity>(jsonArray.GetArrayLength());
        foreach (var entityJson in jsonArray.EnumerateArray())
        {
            TEntity entity;

            if (typeof(TEntity) == typeof(DiscordChannel))
                entity = entityJson.ToChannelEntity(botInstance) as TEntity;
            else
                entity = Activator.CreateInstance(typeof(TEntity), botInstance, entityJson) as TEntity;

            dictionary.Add(entity.Id, entity);
        }

        return dictionary;
    }
}
