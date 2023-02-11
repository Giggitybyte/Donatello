namespace Donatello.Entity;

using System;
using System.Text.Json;

public sealed class UserActivity : IJsonEntity
{
    private readonly JsonElement _json;

    internal UserActivity(JsonElement json)
    {
        _json = json;
    }

    /// <summary></summary>
    internal JsonElement Json => _json;

    /// <summary>The name of the activity.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>A summary of what the user is doing.</summary>
    public string Details => this.Json.TryGetProperty("details", out JsonElement prop) ? prop.ToString() : string.Empty;

    /// <summary></summary>
    public bool HasTimestamp(out DateTime startTime, out DateTime endTime)
    {
        JsonElement timestampJson;
        startTime = DateTime.MinValue;
        endTime = DateTime.MinValue;

        if (this.Json.TryGetProperty("timestamps", out timestampJson) is false)
            return false;

        if (timestampJson.TryGetProperty("start", out JsonElement startJson))
            startTime = startJson.GetDateTime();

        if (timestampJson.TryGetProperty("end", out JsonElement endJson))
            endTime = endJson.GetDateTime();

        return startTime != DateTime.MinValue | endTime != DateTime.MinValue;
    }

    JsonElement IJsonEntity.Json => this.Json;
}
