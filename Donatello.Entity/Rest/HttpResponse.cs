namespace Donatello.Core.Rest;

using System.Net;
using System.Text.Json;

public sealed class HttpResponse
{
    /// <summary></summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary></summary>
    public string Message { get; internal init; }

    /// <summary></summary>
    public JsonElement Payload { get; internal init; }
}
