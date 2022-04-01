namespace Donatello;

using Donatello.Entity;
using System;
using System.Text;
using System.Text.Json;

internal static class InternalExtensionMethods
{
    /// <summary>Converts a JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this JsonElement jsonObject)
        => new StringContent(jsonObject.ToString());

    /// <summary>Converts the contents of a JSON writer to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this Action<Utf8JsonWriter> jsonWriter)
    {
        using var jsonStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(jsonStream);

        writer.WriteStartObject();
        jsonWriter(writer);
        writer.WriteEndObject();

        writer.Flush();
        jsonStream.Seek(0, SeekOrigin.Begin);

        return new StringContent
        (
            new StreamReader(jsonStream).ReadToEnd(),
            Encoding.UTF8,
            "application/json"
        );
    }

    /// <summary>Converts the key-value pairs contained in a tuple array to a URL query parameter string.</summary>
    internal static string ToParamString(this (string key, string value)[] paramArray)
    {
        if (paramArray is null || paramArray.Length is 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var param in paramArray)
        {
            if (builder.Length > 0)
                builder.Append('&');
            else
                builder.Append('?');

            builder.Append(param.key);
            builder.Append('=');
            builder.Append(param.value);
        }

        return builder.ToString();
    }

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static DiscordChannel ToChannelEntity(this JsonElement jsonObject, Bot botInstance)
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
    internal static T[] ToEntityArray<T>(this JsonElement jsonArray, Bot botInstance) where T : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

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

    /// <summary>Deserializes the JSON property as string and converts the value to <see langword="ulong"/>.</summary>
    internal static ulong AsUInt64(this JsonElement jsonProperty) // TODO: proper snowflake type.
    {
        if (jsonProperty.ValueKind is JsonValueKind.String)
            return ulong.Parse(jsonProperty.GetString());
        else
            throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");
    }
}

