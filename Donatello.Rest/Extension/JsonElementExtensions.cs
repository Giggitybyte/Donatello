namespace Donatello.Rest.Extension;

using System.Net.Http;
using System.Text.Json;

/// <summary>Extensions for JSON objects.</summary>
internal static class JsonElementExtensions
{
    /// <summary>Converts the JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this JsonElement jsonObject)
        => new(jsonObject.ToString());
}
