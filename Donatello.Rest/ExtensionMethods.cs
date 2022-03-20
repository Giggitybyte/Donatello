namespace Donatello.Rest;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>Internal helper methods.</summary>
internal static class ExtensionMethods
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

    /// <summary>Converts the key-value pairs in this dictionary to a URL query parameter string.</summary>
    internal static string ToParamString(this IDictionary<string, string> dictionary)
    {
        if (dictionary is null || dictionary.Count is 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var param in dictionary)
        {
            if (builder.Length > 0)
                builder.Append('&');
            else
                builder.Append('?');

            builder.Append(param.Key);
            builder.Append('=');
            builder.Append(param.Value);
        }

        return builder.ToString();
    }
}
