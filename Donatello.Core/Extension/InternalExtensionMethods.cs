﻿namespace Donatello.Extension.Internal;

using Donatello.Entity;
using Donatello.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

internal static class InternalExtensionMethods
{
    /// <summary>Converts this JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is not JsonValueKind.Object)
            throw new JsonException($"Expected an object; got {jsonObject.ValueKind} instead.");

        return new StringContent(jsonObject.GetRawText(), Encoding.UTF8, "application/json");
    }

    /// <summary>Creates a <see cref="StringContent"/> object for REST requests using this delegate.</summary>
    internal static StringContent ToContent(this Action<Utf8JsonWriter> jsonDelegate)
    {
        using var jsonStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(jsonStream);

        writer.WriteStartObject();
        jsonDelegate(writer);
        writer.WriteEndObject();

        writer.Flush();
        jsonStream.Seek(0, SeekOrigin.Begin);

        var json = new StreamReader(jsonStream).ReadToEnd();
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>Converts the key-value pairs contained in a <see cref="ValueTuple"/> array to a URL query parameter string.</summary>
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

    /// <summary>Deserializes the JSON property as string and converts the value to <see langword="ulong"/>.</summary>
    internal static ulong ToUInt64(this JsonElement jsonProperty)
    {
        if (jsonProperty.ValueKind is not JsonValueKind.String)
            throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");

        return ulong.Parse(jsonProperty.GetString());
    }

    /// <summary>Creates a new entity</summary>
    internal static T ToEntity<T>(this JsonElement jsonObject, DiscordApiBot botInstance) where T : DiscordEntity
    {
        if (typeof(T) == typeof(DiscordChannel))
            return jsonObject.ToChannelEntity(botInstance) as T;
        else
            return Activator.CreateInstance(typeof(T), botInstance, jsonObject) as T;
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
            14 => throw new NotImplementedException("Ahahah"),
            _ => throw new JsonException("Unknown channel type.")
        };

        return channel;
    }

    /// <summary>Converts a JSON array to an array of Discord entities.</summary>
    internal static T[] ToEntityArray<T>(this JsonElement jsonArray, DiscordApiBot botInstance) where T : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

        var array = new T[jsonArray.GetArrayLength()];
        int index = 0;

        foreach (var jsonElement in jsonArray.EnumerateArray())
            array[index++] = jsonElement.ToEntity<T>(botInstance);

        return array;
    }

    /// <summary>
    /// Converts each element in a JSON array to a <typeparamref name="TEntity"/> and returns 
    /// the collection as a dictionary, where the key is the snowflake ID of each entity value.
    /// </summary>
    internal static Dictionary<ulong, TEntity> ToEntityDictionary<TEntity>(this JsonElement jsonArray, DiscordApiBot botInstance) where TEntity : DiscordEntity
    {
        if (jsonArray.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonArray.ValueKind} instead.");

        var dictionary = new Dictionary<ulong, TEntity>(jsonArray.GetArrayLength());
        foreach (var jsonElement in jsonArray.EnumerateArray())
        {
            TEntity entity = jsonElement.ToEntity<TEntity>(botInstance);
            dictionary.Add(entity.Id, entity);
        }

        return dictionary;
    }
}