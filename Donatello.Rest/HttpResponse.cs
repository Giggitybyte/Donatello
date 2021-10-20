namespace Donatello.Rest;
using System.Net;
using System.Text.Json;

public readonly struct HttpResponse
{
    /// <summary></summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary></summary>
    public JsonElement? Payload { get; internal init; }
}
