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
    internal static string[] ToStringArray(this JsonElement jsonStrings)
    {
        if (jsonStrings.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array; got {jsonStrings.ValueKind} instead.");

        var array = new string[jsonStrings.GetArrayLength()];
        var index = 0;

        foreach (var jsonElement in jsonStrings.EnumerateArray())
            array[index++] = jsonElement.GetString();

        return array;
    }

    /// <summary>Deserializes the JSON property as string and converts the value to a <see cref="Snowflake"/>.</summary>
    internal static Snowflake ToSnowflake(this JsonElement snowflakeJson)
    {
        if (snowflakeJson.ValueKind is JsonValueKind.Null) return null;
        if (snowflakeJson.ValueKind is not JsonValueKind.String) throw new JsonException($"Expected a string, got {snowflakeJson.ValueKind} instead.");
        return ulong.Parse(snowflakeJson.GetString()!);
    }

    /// <summary></summary>
    internal static Snowflake ToSnowflake(this JsonValue jsonNode)
        => jsonNode.TryGetValue(out string value) ? ulong.Parse(value) : throw new JsonException($"Expected a string.");

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

    /// <summary>Converts a JSON object to an appropriate channel entity.</summary>
    internal static IChannel AsChannel(this JsonElement channelJson, Bot botInstance)
    {
        if (channelJson.ValueKind is JsonValueKind.Undefined) return null;
        if (channelJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected a JSON object, got {channelJson.ValueKind} instead.", nameof(channelJson));
        
        int type = channelJson.GetProperty("type").GetInt32();
        IChannel instance = type switch
        {
            0 => new GuildTextChannel(channelJson, botInstance),
            1 => new DirectTextChannel(channelJson, botInstance),
            2 => new GuildVoiceChannel(channelJson, botInstance),
            3 => throw new ArgumentException("Group DMs are not supported.", nameof(channelJson)),
            4 => new GuildCategoryChannel(channelJson, botInstance),
            5 => new GuildNewsChannel(channelJson, botInstance),
            10 or 11 or 12 => new GuildThreadChannel(channelJson, botInstance),
            13 => new GuildStageChannel(channelJson, botInstance),
            14 => new HubDirectoryChannel(channelJson, botInstance),
            15 => new GuildForumChannel(channelJson, botInstance),
            _ => throw new JsonException("Unknown channel type.")
        };

        return instance;
    }
    
    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(this JsonElement channelJson, Bot botInstance) where TChannel : class, IChannel
    {
        IChannel instance = AsChannel(channelJson, botInstance);
        if (instance is null) return null;
        if (instance is TChannel channel) return channel;
        
        throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal static TChannel AsChannel<TChannel>(this JsonObject channelObject, Bot botInstance) where TChannel : class, IChannel
        => AsChannel<TChannel>(channelObject.AsElement(), botInstance);
    
    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal static IChannel AsChannel(this JsonObject channelJson, Bot botInstance)
        => AsChannel<IChannel>(channelJson, botInstance);

    /// <summary>Converts a JSON object to an appropriate guild channel entity.</summary> 
    internal static IChannel AsChannel(this JsonObject channelObject, Snowflake guildId, Bot botInstance)
    {
        int type = channelObject["type"]!.AsValue().GetValue<int>();
        if (type is 1 or 3) throw new ArgumentException("Expected a guild channel object, got DM channel object instead.");
        if (type is 14) throw new ArgumentException("Expected a guild channel object, got hub channel object instead.");
        
        return type switch
        {
            0 => new GuildTextChannel(channelObject.AsElement(), guildId, botInstance),
            2 => new GuildVoiceChannel(channelObject.AsElement(), guildId, botInstance),
            4 => new GuildCategoryChannel(channelObject.AsElement(), guildId, botInstance),
            5 => new GuildNewsChannel(channelObject.AsElement(), guildId, botInstance),
            10 or 11 or 12 => new GuildThreadChannel(channelObject.AsElement(), guildId, botInstance),
            13 => new GuildStageChannel(channelObject.AsElement(), guildId, botInstance),
            15 => new GuildForumChannel(channelObject.AsElement(), guildId, botInstance),
            _ => throw new JsonException("Invalid channel type.")
        };
    }

    /// <summary>Converts a JSON object to an appropriate guild channel entity.</summary> 
    internal static TChannel AsChannel<TChannel>(this JsonObject channelObject, Snowflake guildId, Bot botInstance) where TChannel : GuildChannel
    {
        IChannel instance = AsChannel(channelObject, guildId, botInstance);
        if (instance is null) return null;
        if (instance is TChannel channel) return channel;
        
        throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }
}
