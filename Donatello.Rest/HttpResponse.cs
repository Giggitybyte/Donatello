namespace Donatello.Rest;

using System.Net;
using System.Text.Json;

/// <summary></summary>
public sealed class HttpResponse
{
    /// <summary>Response status code.</summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary>Response status message.</summary>
    public string Message { get; internal init; }

    /// <summary>JSON response payload.</summary>
    public JsonElement Payload { get; internal init; }
}
