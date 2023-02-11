namespace Donatello.Rest;

using System.IO;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System;

/// <summary></summary>
public sealed class HttpRequest
{
    private string _endpoint;
    private HttpMethod _method;
    private MemoryStream _jsonStream;
    private Utf8JsonWriter _jsonWriter;
    private List<FileAttachment> _files;

    public HttpRequest()
    {
        _jsonStream = new MemoryStream();
        _jsonWriter = new Utf8JsonWriter(_jsonStream);
        _files = new List<FileAttachment>();
    }

    public HttpRequest(HttpMethod method, string endpoint)
        : this()
    {
        _method = method;
        _endpoint = endpoint;
    }

    public HttpRequest(HttpMethod method, string endpoint, JsonElement payload)
        : this(method, endpoint)
    {
        payload.WriteTo(_jsonWriter);
    }

    /// <summary></summary>
    internal HttpMethod Method => _method;

    /// <summary></summary>
    internal string Endpoint => _endpoint;

    /// <summary></summary>
    internal HttpRequestMessage Message => (HttpRequestMessage)this;

    /// <summary>Discord endpoint to send the request to.</summary>
    public HttpRequest SetEndpoint(string endpoint)
    {
        _endpoint = endpoint.Trim('/');
        return this;
    }

    /// <summary>The desired action to be performed.</summary>
    public HttpRequest SetMethod(HttpMethod method)
    {
        _method = method;
        return this;
    }

    /// <summary></summary>
    public HttpRequest WriteJson(Action<Utf8JsonWriter> jsonDelegate)
    {
        jsonDelegate(_jsonWriter);
        return this;
    }

    /// <summary>Adds a file attachment to the request.</summary>
    public HttpRequest AppendFile(FileAttachment file)
    {
        _files.Add(file);
        return this;
    }

    /// <summary>Adds a file attachment from a byte array to this request.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="bytes">File bytes.</param>
    public HttpRequest AppendFile(string fileName, byte[] bytes)
    {
        var attachment = new FileAttachment()
        {
            Name = fileName,
            Size = bytes.LongLength,
            Content = new ByteArrayContent(bytes)
        };

        _files.Add(attachment);
        return this;
    }

    /// <summary>Adds a file attachment from a stream to this request.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="stream">File stream.</param>
    public HttpRequest AppendFile(string fileName, Stream stream)
    {
        var attachment = new FileAttachment()
        {
            Name = fileName,
            Size = stream.Length,
            Content = new StreamContent(stream)
        };

        _files.Add(attachment);
        return this;
    }

    public static implicit operator HttpRequestMessage(HttpRequest request)
    {
        HttpRequestMessage requestMessage = new(request._method, request._endpoint);
        StringContent jsonContent = null;

        request._jsonWriter.Flush();

        if (request._jsonStream.Length is not 0)
        {
            var jsonString = Encoding.UTF8.GetString(request._jsonStream.ToArray());
            jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        }

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
