namespace Donatello.Rest;

using System.Net;
using System.Text.Json;

public readonly struct HttpResponse
{
    public HttpStatusCode Status { get; internal init; }
    public JsonElement? Payload { get; internal init; }
}
