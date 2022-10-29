namespace Donatello.Rest;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

/// <summary></summary>
public sealed class HttpResponse
{
    public readonly struct Error
    {
        /// <summary></summary>
        public string ParameterName { get; internal init; }

        /// <summary></summary>
        public string Code { get; internal init; }

        /// <summary></summary>
        public string Message { get; internal init; }
    }

    internal HttpResponse(ICollection<Error> errors = null)
    {
        this.Errors = errors ?? Array.Empty<Error>();
    }

    /// <summary></summary>
    internal ICollection<Error> Errors { get; init; }

    /// <summary>Response status code.</summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary>Response status message.</summary>
    public string Message { get; internal init; }

    /// <summary>JSON response payload.</summary>
    public JsonElement Payload { get; internal init; }

    /// <summary></summary>
    public bool HasErrors(out ICollection<Error> errors)
    {
        errors = this.Errors;
        return this.Errors.Count > 0;
    }
}
