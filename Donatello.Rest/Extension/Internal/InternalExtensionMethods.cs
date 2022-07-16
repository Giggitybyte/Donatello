namespace Donatello.Rest.Extension.Internal;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class InternalExtensionMethods
{
    /// <summary></summary>
    internal static async Task<JsonElement> GetJsonAsync(this Task<HttpResponse> requestTask)
    {
        var response = await requestTask;

        if (response.Status is HttpStatusCode.OK or HttpStatusCode.NoContent)
            return response.Payload;
        else
        {
            var exceptionMessage = new StringBuilder();

            if (response.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                exceptionMessage.Append(response.Payload.GetProperty("message").GetString());
            else if (response.HasErrors(out var errors))
            {
                foreach (var error in errors)
                {
                    exceptionMessage.AppendLine(error.ParameterName + ": ");
                    exceptionMessage.Append("    ").AppendLine(error.Message);
                    exceptionMessage.Append("    ").AppendLine(error.Code);
                }
            }
            else
                exceptionMessage.Append(response.Message);


            throw new HttpRequestException($"Discord returned an error:\n\n{exceptionMessage}");
        }
    }

    /// <summary>Converts the key-value pairs contained in a <see cref="ValueTuple"/> array to a URL query parameter string.</summary>
    internal static string ToParamString(this (string key, string value)[] paramArray)
    {
        if (paramArray is null || paramArray.Length is 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var param in paramArray)
        {
            if (builder.Length > 0)
                builder.Append('&');
            else
                builder.Append('?');

            builder.Append(param.key);
            builder.Append('=');
            builder.Append(param.value);
        }

        return builder.ToString();
    }
}

