namespace Donatello.Interactions.Builder;

using Donatello.Rest.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary></summary>
public sealed class EmbedPayloadBuilder : PayloadBuilder
{
    private string _title, _description, _footer;
    private List<Field> _fields;

    internal EmbedPayloadBuilder() 
        => _fields = new List<Field>(25);

    internal override void Build(Utf8JsonWriter json)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedPayloadBuilder AppendField(string title, string content, bool inline = false)
    {
        if (_fields.Count + 1 > _fields.Capacity)
            throw new InvalidOperationException($"Embed can only contain {_fields.Capacity} fields.");

        _fields.Add(new Field
        {
            Title = title,
            Content = content,
            IsInline = inline
        });

        return this;
    }

    /// <summary></summary>
    public EmbedPayloadBuilder SetTitle(string title)
    {
        if (title.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(title), "Title cannot be greater than 256 characters.");

        _title = title;
        return this;
    }

    /// <summary></summary>
    public EmbedPayloadBuilder SetDescription(string description)
    {
        if (description.Length > 4096)
            throw new ArgumentOutOfRangeException(nameof(description), "Description cannot be greater than 4096 characters.");

        _description = description;
        return this;
    }

    public EmbedPayloadBuilder SetFooter(string footer)
    {

    }

    public EmbedPayloadBuilder SetFooter(string footer, FileAttachment icon)
    {

    }

    public EmbedPayloadBuilder SetFooter(string footer, Uri iconUrl)
    {

    }


    /// <summary></summary>
    public EmbedPayloadBuilder SetThumbnail(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedPayloadBuilder SetThumbnail(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedPayloadBuilder SetImage(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedPayloadBuilder SetImage(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    private struct Field
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public bool IsInline { get; set; }
    }
}
