namespace Donatello.Common;

using System.Collections.Generic;
using System.Text.Json;

public sealed class Emoji
{
    public Emoji(JsonElement metadata)
    {
        var jsonArray = metadata.GetProperty("names");
        var names = new string[jsonArray.GetArrayLength()];
        var index = 0;
        
        foreach (var json in jsonArray.EnumerateArray())
            names[index++] = json.GetString()!;

        this.Names = names;
        this.Unicode = metadata.GetProperty("surrogates").GetString()!;
        this.Shortcode = metadata.GetProperty("primaryNameWithColons").GetString()!;
        this.Category = metadata.GetProperty("category").GetString()!;
        this.ImageUrl = metadata.GetProperty("assetUrl").GetString()!;
    }

    /// <summary>The raw unicode emoji which this object represents.</summary>
    public string Unicode { get; }

    /// <summary>Human-readable string of short and succinct words, surrounded by colons, which represent the emoji.</summary>
    public string Shortcode { get; }

    /// <summary>A general description of what the emoji represents.</summary>
    public string Category { get; }

    /// <summary>Direct URL to an image of the emoji.</summary>
    public string ImageUrl { get; }

    /// <summary>Collection containing each name or emoticon used to reference the emoji.</summary>
    public IList<string> Names { get; }
}