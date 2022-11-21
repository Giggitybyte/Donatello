namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class Embed
{
    private JsonElement _json;

    internal Embed(JsonElement json)
    {
        _json = json;
    }

    /// <summary></summary>
    /// <param name="title"></param>
    public bool HasTitle(out string title)
    {
        title = _json.TryGetProperty("title", out JsonElement prop) 
            ? prop.ToString() 
            : string.Empty;

        return title != string.Empty;
    }

    /// <summary></summary>
    /// <param name="description"></param>
    public bool HasDescription(out string description)
    {
        description = _json.TryGetProperty("description", out JsonElement prop) 
            ? prop.ToString() 
            : string.Empty;

        return description != string.Empty;
    }
}

