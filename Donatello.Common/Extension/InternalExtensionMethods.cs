namespace Donatello.Extension.Internal;

using Donatello;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

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
}
