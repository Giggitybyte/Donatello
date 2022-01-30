namespace Donatello.Interactions.Model.Builder;

using Donatello.Rest.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary></summary>
public sealed class EmbedBuilder : EntityBuilder
{
    private List<Field> _fields;

    internal EmbedBuilder()
    {
        _fields = new List<Field>(25);
    }

    /// <summary></summary>
    public EmbedBuilder AppendField(string title, string content, bool inline = false)
    {
        if ((_fields.Count + 1) > _fields.Capacity)
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
    public EmbedBuilder SetTitle(string title)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public EmbedBuilder SetDescription(string title)
    {
        throw new NotImplementedException();
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

    internal override void Build(Utf8JsonWriter json)
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
