namespace Donatello.Rest.Extension;

using System;
using System.Buffers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;

internal static class InternalExtensionMethods
{
    /// <summary>Converts key-value pairs contained in a <see cref="ValueTuple"/> array to a URL query parameter string.</summary>
    /// <remarks><see langword="default"/> parameters as well as parameters with <see langword="null"/> keys will be ignored.</remarks>
    internal static string ToParamString(this (string key, string value)[] paramArray)
    {
        if (paramArray == default || paramArray.Length is 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var parameter in paramArray)
        {
            if (parameter == default || string.IsNullOrEmpty(parameter.key))
                continue;

            if (builder.Length > 0)
                builder.Append('&');
            else
                builder.Append('?');

            builder.Append(parameter.key);
            builder.Append('=');
            builder.Append(parameter.value);
        }

        return builder.ToString();
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

