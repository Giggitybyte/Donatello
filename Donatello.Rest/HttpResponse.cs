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
        public int Code { get; internal init; }

        /// <summary></summary>
        public string Message { get; internal init; }
    }

    /// <summary>Response status code.</summary>
    public HttpStatusCode Status { get; internal init; }

    /// <summary>Response status message.</summary>
    public string Message { get; internal init; }

    /// <summary>JSON response payload.</summary>
    public JsonElement Payload { get; internal init; }

    /// <summary></summary>
    public bool HasErrors(out ICollection<Error> errors)
    {
        if (this.Payload.ValueKind is JsonValueKind.Undefined)
        {
            errors = Array.Empty<Error>();
            return false;
        }

        var errorMessages = new List<Error>();
        
        if (this.Payload.ValueKind is JsonValueKind.Object)
        {
            // TODO: array error.

            if (this.Payload.TryGetProperty("errors", out JsonElement errorObject)) // TODO: I think this logic is broke a little.
            {
                if (errorObject.TryGetProperty("_errors", out JsonElement errorProp))
                    AddErrors(errorProp, "request");
                else
                {
                    foreach (var objectProp in errorObject.EnumerateObject())
                        foreach (var errorJson in objectProp.Value.GetProperty("_errors").EnumerateArray())
                            AddErrors(errorJson, objectProp.Name);
                }
            }
            else if (this.Payload.TryGetProperty("message", out JsonElement messageProp))
            {
                var error = new Error
                {
                    ParameterName = string.Empty,
                    Code = this.Payload.GetProperty("code").GetInt32(),
                    Message = messageProp.GetString()
                };

                errorMessages.Add(error);
            }
        }

        errors = errorMessages;
        return errors.Count is not 0;
        
        void AddErrors(JsonElement errorArray, string name)
        {
            foreach (var errorJson in errorArray.EnumerateArray())
            {
                var error = new Error()
                {
                    ParameterName = name,
                    Code = errorJson.GetProperty("code").GetInt32(),
                    Message = errorJson.GetProperty("message").GetString()
                };

                errorMessages.Add(error);
            }
        }
    }
}
