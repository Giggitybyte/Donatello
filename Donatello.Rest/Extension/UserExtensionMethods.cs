namespace Donatello.Rest.Extension;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class UserExtensionMethods
{
    /// <summary>Returns the resulting <see cref="HttpResponse.Payload"/>, or throws <see cref="HttpRequestException"/> if a non-success response code was returned.</summary>
    public static async Task<JsonElement> GetJsonAsync(this Task<HttpResponse> requestTask)
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

    /// <summary>Yields each JSON object contained within the resulting <see cref="HttpResponse.Payload"/></summary>
    /// <remarks>Throws <see cref="HttpRequestException"/> if a non-success response code was returned.</remarks>
    public static async IAsyncEnumerable<JsonElement> GetJsonArrayAsync(this Task<HttpResponse> requestTask)
    {
        var array = await requestTask.GetJsonAsync();

        if (array.ValueKind is not JsonValueKind.Array)
            throw new JsonException($"Expected an array, got {array.ValueKind} instead.");

        foreach (var json in array.EnumerateArray())
            yield return json;
    }
}

