namespace Donatello.Interactions.Model.Builder;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary></summary>
public sealed class MessageBuilder : EntityBuilder
{
    private string _content;
    private List<EmbedBuilder> _embedBuilders;

    public MessageBuilder SetContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot be greater than 2,000 characters", nameof(content));

        _content = content;
        return this;
    }

    public MessageBuilder AddEmbed(Action<EmbedBuilder> embed)
    {
        var builder = new EmbedBuilder();
        embed(builder);


        _embedBuilders.Add(builder);
        return this;
    }

    internal override Stream ToJsonStream()
    {
        this.JsonWriter.WriteString("content", _content);
        return base.ToJsonStream();
    }
}
