namespace Donatello.Rest.Extension.Json;

using System.Net.Http;
using System.Text.Json;

internal static class JsonElementExtensions
{
    /// <summary>Converts the JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    internal static StringContent ToContent(this JsonElement jsonObject)
        => new(jsonObject.ToString());
}
