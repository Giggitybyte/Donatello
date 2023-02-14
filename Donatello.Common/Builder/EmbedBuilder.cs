namespace Donatello.Common.Builder;

using System;
using System.Text.Json.Nodes;
using Donatello.Rest;

/// <summary></summary>
public sealed class EmbedBuilder : EntityBuilder
{
    /// <summary></summary>
    public EmbedBuilder SetTitle(string title)
    {
        if (title.Length <= 256)
            this.Json["title"] = title;
        else
            throw new ArgumentOutOfRangeException(nameof(title), "Title cannot be greater than 256 characters.");

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetDescription(string description)
    {
        if (description.Length <= 4096)
            this.Json["description"] = description;
        else
            throw new ArgumentOutOfRangeException(nameof(description), "Description cannot be greater than 4096 characters.");

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetUrl(Uri url)
    {
        this.Json["url"] = url.ToString();
        return this;
    }

    /// <summary></summary>
    public EmbedBuilder AppendField(string name, string content, bool inline = false)
    {
        var fieldNode = this.Json["fields"] ??= new JsonArray();
        var fieldArray = fieldNode.AsArray();

        if (fieldArray.Count + 1 <= 25)
            fieldArray.Add(new JsonObject()
            {
                { "name", name },
                { "value", content },
                { "inline", inline }
            });
        else
            throw new InvalidOperationException("Embed can only contain 25 fields.");

        return this;
    }

    /// <summary></summary>
    public void ClearFields()
        => this.Json["fields"]?.AsArray()?.Clear();

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer)
    {
        this.Json["footer"] ??= new JsonObject();
        this.Json["footer"]["text"] = footer;

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer, string iconUrl)
    {
        this.SetFooter(footer);
        this.Json["footer"]["icon_url"] = iconUrl;

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetFooter(string footer, FileAttachment icon)
    {
        this.Files.Add(icon);
        return this.SetFooter(footer, $"attachment://{icon.Name}");
    }

    /// <summary></summary>
    public EmbedBuilder SetThumbnail(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetThumbnail(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetImage(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetImage(Uri imageUrl)
    {
        throw new NotImplementedException();
    }
}
