namespace Donatello.Builder;

using Donatello.Entity;
using Donatello.Rest;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary></summary>
public sealed class EmbedBuilder : JsonObjectBuilder<DiscordMessage.Embed>
{
    /// <summary></summary>
    public EmbedBuilder SetTitle(string title)
    {
        if (title.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(title), "Title cannot be greater than 256 characters.");

        this.Json["title"] = title;

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetDescription(string description)
    {
        if (description.Length > 4096)
            throw new ArgumentOutOfRangeException(nameof(description), "Description cannot be greater than 4096 characters.");

        this.Json["description"] = description;

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetUrl(Uri url)
    {
        this.Json["url"] = url.ToString();
        return this;
    }

    /// <summary></summary>
    public EmbedBuilder AppendField(string title, string content, bool inline = false)
    {
        this.Json["fields"] ??= new JsonArray();
        var fields = this.Json["fields"].AsArray();

        if (fields.Count + 1 > 25)
            throw new InvalidOperationException("Embed can only contain 25 fields.");

        fields.Add(new JsonObject()
        {
            { "name", title },
            { "value", content },
            { "inline", inline }
        });

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer)
    {

    }

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer, Attachment icon)
    {

    }

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer, Uri iconUrl)
    {

    }

    /// <summary></summary>
    public EmbedBuilder SetThumbnail(Attachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetThumbnail(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetImage(Attachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetImage(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    internal override void ConstructJson(in Utf8JsonWriter json)
    {
        throw new NotImplementedException();
    }
}
