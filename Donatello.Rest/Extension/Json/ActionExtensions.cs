namespace Donatello.Rest.Extension.Json;

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>Extensions for JSON builder functions (lambdas, method references).</summary>
internal static class ActionExtensions
{
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
}
