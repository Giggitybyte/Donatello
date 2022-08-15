﻿namespace Donatello.Entity.Builder;

using Donatello.Rest;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary></summary>
public sealed class EmbedBuilder : EntityBuilder
{
    private struct Field
    {
        public string Title { get; init; }
        public string Content { get; init; }
        public bool IsInline { get; init; }
    }

    private string _title, _description, _footer;
    private List<Field> _fields;

    /// <summary></summary>
    public EmbedBuilder()
    {
        _fields = new List<Field>(25);
    }

    /// <summary></summary>
    public EmbedBuilder AppendField(string title, string content, bool inline = false)
    {
        if (_fields.Count + 1 > _fields.Capacity)
            throw new InvalidOperationException($"Embed can only contain {_fields.Capacity} fields.");
        
        _fields.Add(new Field(title, content, inline));

        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetTitle(string title)
    {
        if (title.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(title), "Title cannot be greater than 256 characters.");

        _title = title;
        return this;
    }

    /// <summary></summary>
    public EmbedBuilder SetDescription(string description)
    {
        if (description.Length > 4096)
            throw new ArgumentOutOfRangeException(nameof(description), "Description cannot be greater than 4096 characters.");

        _description = description;
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
