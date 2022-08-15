﻿namespace Donatello.Rest;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public enum TokenType : ushort
{
    /// <summary></summary>
    Bot,

    /// <summary></summary>
    Bearer
}

/// <summary>HTTP client wrapper for the Discord REST API with integrated rate-limiter.</summary>
public class DiscordHttpClient
{
    private HttpClient _client;
    private ConcurrentDictionary<Uri, string> _routeBucketIds;
    private ConcurrentDictionary<string, RatelimitBucket> _routeBuckets;
    private RatelimitBucket _globalBucket;

    /// <param name="token">Discord token.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="tokenType"><see langword="true"/>: OAuth2 bearer token.<br/><see langword="false"/>: application bot token.</param>
    public DiscordHttpClient(TokenType tokenType, string token, ILogger logger = null)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://discord.com/api/v10");
        _client.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (author: 176477523717259264)");
        _client.DefaultRequestHeaders.Add("Authorization", $"{(tokenType is TokenType.Bot ? "Bot" : "Bearer")} {token}");

        _routeBucketIds = new ConcurrentDictionary<Uri, string>();
        _routeBuckets = new ConcurrentDictionary<string, RatelimitBucket>();
        _globalBucket = new RatelimitBucket("global")
        {
            Limit = 50,
            Remaining = 50
        };

        this.Logger = logger ?? NullLogger.Instance;
    }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Sends an HTTP request to an endpoint.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint)
        => SendRequestCoreAsync(method, endpoint);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonDelegate)
        => SendRequestCoreAsync(method, endpoint, CreateStringContent(jsonDelegate));

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject)
        => SendRequestCoreAsync(method, endpoint, CreateStringContent(jsonObject));

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, Action<Utf8JsonWriter> jsonDelegate, IList<Attachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, CreateStringContent(jsonDelegate), attachments);

    /// <summary>Sends an HTTP request to an endpoint with a JSON payload and file attachments.</summary>
    public Task<HttpResponse> SendRequestAsync(HttpMethod method, string endpoint, JsonElement jsonObject, IList<Attachment> attachments)
        => SendMultipartRequestAsync(method, endpoint, CreateStringContent(jsonObject), attachments);

    /// <summary>Sends a multi-part HTTP request to an endpoint.</summary>
    private Task<HttpResponse> SendMultipartRequestAsync(HttpMethod method, string endpoint, StringContent content, IList<Attachment> attachments)
    {
        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(content, "payload_json");

        for (int index = 0; index < attachments.Count; index++)
        {
            var attachment = attachments[index];
            multipartContent.Add(attachment.Content, $"files[{index}]", attachment.Name);
        }

        return SendRequestCoreAsync(method, endpoint, multipartContent);
    }

    /// <summary>Sends an HTTP request.</summary>
    private Task<HttpResponse> SendRequestCoreAsync(HttpMethod method, string endpoint, HttpContent content = null)
    {
        var attemptCount = 0;
        var requestDate = DateTimeOffset.UtcNow;
        var request = new HttpRequestMessage(method, endpoint.Trim('/'));
        Task<HttpResponse> requestTask = null;

        if (content is not null)
            request.Content = content;

        if (_globalBucket.TryUse())
        {
            if (_routeBucketIds.TryGetValue(request.RequestUri, out var bucketId) && _routeBuckets.TryGetValue(bucketId, out var routeBucket))
            {
                if (routeBucket.TryUse())
                    requestTask = DispatchRequestAsync();
                else
                    requestTask = DelayRequestAsync(routeBucket.ResetDate - requestDate);
            }
        }
        else
            requestTask = DelayRequestAsync(_globalBucket.ResetDate - requestDate);

        return requestTask;

        async Task<HttpResponse> DispatchRequestAsync()
        {
            this.Logger.LogDebug("Sending request to {Method} {Uri} (attempt #{Attempt})", request.Method.Method, request.RequestUri, ++attemptCount);
            using var response = await _client.SendAsync(request);
            this.Logger.LogDebug("Request to {Url} got {Status} response from Discord.", request.RequestUri.AbsolutePath, response.StatusCode);

            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var values))
                UpdateRatelimitBucket(values.Single(), response.Headers);

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                var scope = response.Headers.GetValues("X-RateLimit-Scope").Single();
                var retrySeconds = int.Parse(response.Headers.GetValues("Retry-After").Single());
                var retryTime = TimeSpan.FromSeconds(retrySeconds);

                if (scope is "global")
                    this.Logger.LogCritical("Hit global rate limit");
                else
                    this.Logger.LogWarning("Hit {Scope} ratelimit for {Url}", scope, request.RequestUri.AbsolutePath);

                return await DelayRequestAsync(retryTime);
            }

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var responseJson = await JsonDocument.ParseAsync(responseStream);

            this.Logger.LogTrace("Request to {Url} completed after {Attempt} attempts.", request.RequestUri.AbsolutePath, attemptCount);

            return new HttpResponse()
            {
                Status = response.StatusCode,
                Message = response.ReasonPhrase,
                Errors = ParseErrorMessages(responseJson.RootElement),
                Payload = responseJson.RootElement.Clone()
            };
        }

        async Task<HttpResponse> DelayRequestAsync(TimeSpan delayTime)
        {
            this.Logger.LogWarning("Request to {Uri} delayed until {Time} (attempt #{Attempt})", request.RequestUri.AbsolutePath, DateTime.Now.Add(delayTime), attemptCount);

            await Task.Delay(delayTime);
            var response = await DispatchRequestAsync();

            return response;
        }

        void UpdateRatelimitBucket(string bucketId, HttpResponseHeaders headers)
        {
            _routeBucketIds[request.RequestUri] = bucketId;

            if (_routeBuckets.TryGetValue(bucketId, out var bucket))
            {
                bucket.Update(headers);
                this.Logger.LogTrace("Updated existing ratelimit bucket {Id}", bucketId);
            }
            else
            {
                bucket = new RatelimitBucket(bucketId, headers);

                _routeBuckets.TryAdd(bucketId, bucket);
                this.Logger.LogTrace("Created new ratelimit bucket {Id}", bucketId);
            }
        }

        IList<HttpResponse.Error> ParseErrorMessages(JsonElement responseJson)
        {
            var errorMessages = new List<HttpResponse.Error>();

            if (responseJson.TryGetProperty("errors", out var errorObject))
            {
                if (errorObject.TryGetProperty("_errors", out var errorProp))
                    AddError(errorProp, "request");
                else
                    foreach (var objectProp in errorObject.EnumerateObject())
                        foreach (var errorJson in objectProp.Value.GetProperty("_errors").EnumerateArray())
                            AddError(errorJson, objectProp.Name);

                void AddError(JsonElement errorArray, string name)
                {
                    foreach (var errorJson in errorArray.EnumerateArray())
                    {
                        var code = errorJson.GetProperty("code").GetString();
                        var message = errorJson.GetProperty("message").GetString();
                        var error = new HttpResponse.Error() 
                        { 
                            ParameterName = name, 
                            Code = code, 
                            Message = message 
                        };

                        errorMessages.Add(error);
                    }
                }
            }
            else if (responseJson.TryGetProperty("message", out var messageProp))
            {
                var code = responseJson.GetProperty("code").GetString();
                var error = new HttpResponse.Error() { ParameterName = string.Empty, Code = code, Message = messageProp.GetString() };
                errorMessages.Add(error);
            }

            return errorMessages;
        }
    }

    /// <summary>Converts this JSON object to a <see cref="StringContent"/> object for REST requests.</summary>
    private StringContent CreateStringContent(JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is not JsonValueKind.Object)
            throw new JsonException($"Expected an object; got {jsonObject.ValueKind} instead.");

        return new StringContent(jsonObject.GetRawText(), Encoding.UTF8, "application/json");
    }

    /// <summary>Creates a <see cref="StringContent"/> object for REST requests using this delegate.</summary>
    private StringContent CreateStringContent(Action<Utf8JsonWriter> jsonDelegate)
    {
        using var jsonStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(jsonStream);

        writer.WriteStartObject();
        jsonDelegate(writer);
        writer.WriteEndObject();

        writer.Flush();
        jsonStream.Seek(0, SeekOrigin.Begin);

        var json = new StreamReader(jsonStream).ReadToEnd();
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}