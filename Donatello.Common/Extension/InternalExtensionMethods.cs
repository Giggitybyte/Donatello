namespace Donatello.Common.Extension;

using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Entity.Channel;
using Entity.Guild.Channel;

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

    /// <summary>Deserializes the JSON property as string and converts the value to a <see cref="Snowflake"/>.</summary>
    internal static Snowflake ToSnowflake(this JsonElement jsonProperty)
    {
        if (jsonProperty.ValueKind is not JsonValueKind.String)
            throw new JsonException($"Expected a string, got {jsonProperty.ValueKind} instead.");

        return ulong.Parse(jsonProperty.GetString());
    }

    /// <summary></summary>
    internal static Snowflake ToSnowflake(this JsonValue jsonNode)
    {
        if (jsonNode.TryGetValue<string>(out var value) is false)
            throw new JsonException($"Expected a string.");

        return ulong.Parse(value);
    }

    /// <summary></summary>
    internal static JsonElement AsElement(this JsonNode jsonNode)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        jsonNode.WriteTo(jsonWriter);
        jsonWriter.Flush();

        using var jsonDoc = JsonDocument.Parse(buffer.WrittenMemory);
        return jsonDoc.RootElement.Clone();
    }
    
            /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(this JsonElement channelJson) where TChannel : class, IChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        Channel instance = type switch
        {
            0 => new GuildTextChannel(channelJson),
            1 => new DirectMessageChannel(channelJson),
            2 => new GuildVoiceChannel(channelJson),
            3 => throw new ArgumentException("Group DMs are not supported.", nameof(channelJson)),
            4 => new GuildCategoryChannel(channelJson),
            5 => new GuildNewsChannel(channelJson),
            10 or 11 or 12 => new GuildThreadChannel(channelJson),
            13 => new GuildStageChannel(channelJson),
            14 => new HubDirectoryChannel(channelJson),
            15 => new GuildForumChannel(channelJson),
            _ => throw new JsonException("Unknown channel type.")
        };

        if (instance is TChannel channel)
            return channel;
        else
            throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }
    
    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(this JsonElement channelJson, Snowflake guildId) where TChannel : class, IGuildChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        Channel instance = type switch
        {
            0 => new GuildTextChannel(channelJson, guildId),
            1 or 3 => throw new InvalidOperationException("DM channels cannot be deserialized with this overload."),
            2 => new GuildVoiceChannel(channelJson, guildId),
            4 => new GuildCategoryChannel(channelJson, guildId),
            5 => new GuildNewsChannel(channelJson, guildId),
            10 or 11 or 12 => new GuildThreadChannel(channelJson, guildId),
            13 => new GuildStageChannel(channelJson, guildId),
            14 => new HubDirectoryChannel(channelJson, guildId),
            15 => new GuildForumChannel(channelJson, guildId),
            _ => throw new JsonException("Unknown channel type.")
        };

        if (instance is TChannel channel)
            return channel;
        else
            throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static Channel AsChannel(this Bot botInstance, JsonElement channelJson)
        => AsChannel<Channel>(channelJson);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(JsonObject channelObject) where TChannel : class, IChannel
        => AsChannel<TChannel>(channelObject.AsElement());

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static Channel AsChannel(JsonObject channelJson)
        => AsChannel<Channel>(channelJson);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(JsonObject channelObject, Snowflake guildId) where TChannel : class, IGuildChannel
        => AsChannel<TChannel>(channelObject.AsElement(), guildId);
}
