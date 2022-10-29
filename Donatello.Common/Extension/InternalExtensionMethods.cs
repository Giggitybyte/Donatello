namespace Donatello.Extension.Internal;

using Donatello.Entity;
using System;
using System.Text.Json;

internal static class InternalExtensionMethods
{
    /// <summary>Converts a JSON array to a native array of strings.</summary>
    internal static string[] ToStringArray(this JsonElement jsonArray)
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

        var array = new string[jsonArray.GetArrayLength()];
        var index = 0;

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

    /// <summary>Adds the provided <typeparamref name="TEntity"/> instance to this cache.</summary>
    internal static void Add(this EntityCache<JsonElement> jsonCache, JsonElement json)
        => jsonCache.Add(json.GetProperty("id").ToSnowflake(), json);

    /// <summary>Removes and returns an existing entry with the same ID as <paramref name="updatedJson"/> after replacing it with <paramref name="updatedJson"/>.</summary>
    internal static JsonElement Replace(this EntityCache<JsonElement> jsonCache, JsonElement updatedJson)
        => jsonCache.Replace(updatedJson.GetProperty("id").ToSnowflake(), updatedJson);

    /// <summary>Adds the provided <typeparamref name="TEntity"/> instance to this cache.</summary>
    internal static void Add<TEntity>(this EntityCache<TEntity> entityCache, TEntity entity) where TEntity : IEntity
        => entityCache.Add(entity.Id, entity);

    /// <summary>Removes and returns an existing entry with the same ID as <paramref name="updatedEntity"/> after replacing it with <paramref name="updatedEntity"/>.</summary>
    internal static TEntity Replace<TEntity>(this EntityCache<TEntity> entityCache, TEntity updatedEntity) where TEntity : IEntity
        => entityCache.Replace(updatedEntity.Id, updatedEntity);
}
