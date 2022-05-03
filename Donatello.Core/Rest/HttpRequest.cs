namespace Donatello.Rest;

using System;
using System.Net.Http;

/// <summary></summary>
public ref struct HttpRequest
{
    /// <summary></summary>
    public HttpMethod Method { get; internal init; }

    /// <summary></summary>
    public string Endpoint { get; internal init; }

    /// <summary></summary>
    public HttpContent Content { get; internal init; }

    /// <summary></summary>
    public uint Attempts { get; internal set; }

    /// <summary></summary>
    public DateTimeOffset LastAttempt { get; internal set; }
}

