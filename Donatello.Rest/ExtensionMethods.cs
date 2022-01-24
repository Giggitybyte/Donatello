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
    /// <summary>Converts the JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this JsonElement jsonObject)
        => new(jsonObject.ToString());

    /// <summary>Converts the builder function to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this Action<Utf8JsonWriter> jsonBuilder)
    {
        using var jsonStream = new MemoryStream();
        var writer = new Utf8JsonWriter(jsonStream);

        writer.WriteStartObject();
        jsonBuilder(writer);
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
        if (dictionary is null)
            return string.Empty;

        var builder = new StringBuilder();
        var index = 0;

        foreach (var param in dictionary)
        {
            if (index++ == 0)
                builder.Append('?');
            else
                builder.Append('&');

            builder.Append(param.Key);
            builder.Append('=');
            builder.Append(param.Value);
        }

        return builder.ToString();
    }
}
