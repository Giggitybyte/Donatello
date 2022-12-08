namespace Donatello.Rest;

using System.IO;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Nodes;

/// <summary></summary>
public class HttpRequest
{
    private string _endpoint;
    private HttpMethod _method;
    private JsonObject _json;
    private List<FileAttachment> _files;

    internal HttpRequest()
    {
        this.Json = new JsonObject();
        _files = new List<FileAttachment>();
    }

    /// <summary></summary>
    public string Endpoint { get => _endpoint; set => _endpoint = value.Trim('/'); }

    /// <summary></summary>
    public HttpMethod Method { get => _method; set => _method = value; }

    /// <summary>Mutable JSON payload to be sent with the request.</summary>
    public JsonObject Json { get => _json; set => _json = value; }

    /// <summary>Adds a file attachment from a byte array to this request.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="bytes">File bytes.</param>
    public void AppendFile(string fileName, byte[] bytes)
        => _files.Add(new FileAttachment() { Name = fileName, Size = bytes.LongLength, Content = new ByteArrayContent(bytes) });

    /// <summary>Adds a file attachment from a stream to this request.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="stream">File stream.</param>
    public void AppendFile(string fileName, Stream stream)
        => _files.Add(new FileAttachment() { Name = fileName, Size = stream.Length, Content = new StreamContent(stream) });

    public static implicit operator HttpRequestMessage(HttpRequest request)
    {
        HttpRequestMessage requestMessage = new(request.Method, request.Endpoint);
        StringContent jsonContent = null;

        if (request.Json.Count is not 0)
            jsonContent = new StringContent(request.Json.ToJsonString(), Encoding.UTF8, "application/json");

        if (request._files.Count is not 0)
        {
            var multipartContent = new MultipartFormDataContent();

            if (jsonContent is not null)
                multipartContent.Add(jsonContent, "payload_json");

            for (int index = 0; index < request._files.Count; index++)
            {
                var attachment = request._files[index];
                multipartContent.Add(attachment.Content, $"files[{index}]", attachment.Name);
            }

            requestMessage.Content = multipartContent;
        }
        else if (jsonContent is not null)
            requestMessage.Content = jsonContent;

        return requestMessage;
    }
}
