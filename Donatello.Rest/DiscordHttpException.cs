namespace Donatello.Rest;

using System;
using System.Net;
using System.Text.Json;

/// <summary></summary>
public sealed class DiscordHttpException : Exception
{
    /// <summary></summary>
    internal DiscordHttpException(HttpStatusCode status, string message) : base(message)
        => this.Status = status;

    /// <summary></summary>
    internal DiscordHttpException(HttpStatusCode status, string message, JsonElement jsonMessage) : this(status, message)
        => this.JsonMessage = jsonMessage;

    /// <summary></summary>
    public HttpStatusCode Status { get; private init; }

    /// <summary></summary>
    public JsonElement? JsonMessage { get; private init; }
}
