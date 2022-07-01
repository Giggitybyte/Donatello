namespace Donatello.Rest.Extension.Internal;

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public static class InternalExtensionMethods
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
}

