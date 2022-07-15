namespace Donatello.Rest.Extension.Internal;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class InternalExtensionMethods
{
    /// <summary></summary>
    internal static async Task<JsonElement> FetchJson(this DiscordHttpClient httpClient, HttpMethod method, string endpoint)
    {
        var response = await httpClient.SendRequestAsync(method, endpoint);

        if (response.Status is HttpStatusCode.OK)
            return response.Payload;

        if (response.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            throw new ArgumentException(response.Payload.GetProperty("message").GetString());
        else
        {
            var message = new StringBuilder();

            if (response.HasErrors(out var errors))
            {
                foreach (var error in errors)
                {
                    message.AppendLine(error.ParameterName + ": ");
                    message.Append("    ").AppendLine(error.Message);
                    message.Append("    ").AppendLine(error.Code);
                }
            }
            else
                message.Append(response.Message);

            throw new HttpRequestException($"Unable to fetch entity from Discord: {message} ({(int)response.Status})");
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

