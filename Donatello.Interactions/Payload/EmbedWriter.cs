namespace Donatello.Interactions.Payload;

using Donatello.Rest.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary></summary>
public sealed class EmbedWriter : PayloadWriter
{
    private List<Field> _fields;

    internal EmbedWriter()
    {
        _fields = new List<Field>(25);
    }

    internal override void WritePayload(Utf8JsonWriter json)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter AppendField(string title, string content, bool inline = false)
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
    public EmbedWriter SetTitle(string title)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter SetDescription(string title)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter SetThumbnail(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter SetThumbnail(Uri imageUrl)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter SetImage(FileAttachment image)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedWriter SetImage(Uri imageUrl)
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
