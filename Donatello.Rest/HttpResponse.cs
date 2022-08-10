namespace Donatello.Rest;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

/// <summary></summary>
public sealed class HttpResponse
{
    internal HttpResponse()
    {
        this.Errors = Array.Empty<Error>();
    }

    /// <summary></summary>
    internal IList<Error> Errors { get; init; }

    /// <summary>Response status code.</summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary>Response status message.</summary>
    public string Message { get; internal init; }

    /// <summary>JSON response payload.</summary>
    public JsonElement Payload { get; internal init; }

    /// <summary></summary>
    public bool HasErrors(out IEnumerable<Error> errors)
    {
        errors = this.Errors;
        return this.Errors.Count > 0;
    }

    public readonly struct Error
    {
        /// <summary></summary>
        public string ParameterName { get; internal init; }

        /// <summary></summary>
        public string Code { get; internal init; }

        /// <summary></summary>
        public string Message { get; internal init; }
    }
}
