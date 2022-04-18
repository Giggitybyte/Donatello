namespace Donatello.Rest;

using System;
using System.Net.Http;

/// <summary></summary>
public struct HttpRequest
{
    /// <summary></summary>
    public HttpMethod Method { get; internal set; }

    /// <summary></summary>
    public string Endpoint { get; internal set; }

    /// <summary></summary>
    public HttpContent Content { get; internal set; }

    /// <summary></summary>
    public uint Attempts { get; internal set; }

    /// <summary></summary>
    public DateTimeOffset LastAttempt { get; internal set; }
}

