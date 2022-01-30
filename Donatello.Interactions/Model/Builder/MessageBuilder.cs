namespace Donatello.Interactions.Model.Builder;

using Donatello.Interactions.Entity;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary></summary>
public sealed class MessageBuilder : EntityBuilder
{
    private string _content;
    private bool _tts;
    private List<EmbedBuilder> _embedBuilders;
    private List<string> _stickerIds;
    private DiscordMessage _messageReference;
    private MentionConfiguration _mentionConfiguration;

    internal MessageBuilder()
    {
        _tts = false;
        _embedBuilders = new List<EmbedBuilder>(10);
        _mentionConfiguration = new MentionConfiguration();
    }

    /// <summary></summary>
    public MessageBuilder SetContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot be greater than 2,000 characters", nameof(content));

        _content = content;
        return this;
    }

    /// <summary>Whether the message should be spoken aloud using text-to-speech.</summary>
    public MessageBuilder SetTts(bool value)
    {
        _tts = value;
        return this;
    }

    /// <summary></summary>
    public MessageBuilder SetReply(DiscordMessage message)
    {
        _messageReference = message;
        return this;
    }

    /// <summary>Append an embed to the message.</summary>
    public MessageBuilder AddEmbed(Action<EmbedBuilder> embed)
    {
        if (_embedBuilders.Count + 1 > _embedBuilders.Capacity)
            throw new InvalidOperationException($"Message cannot have more than {_embedBuilders.Capacity} embeds.");

        var builder = new EmbedBuilder();

        embed(builder);
        _embedBuilders.Add(builder);

        return this;
    }

    /// <summary></summary>
    public MessageBuilder ConfigureMentions(Action<MentionConfiguration> config)
    {
        throw new NotImplementedException();
    }

    internal override void Build(Utf8JsonWriter json)
    {
        if (_content is null || _embedBuilders.Count is 0 || _stickerIds.Count is 0)
        {

        }

        json.WriteString("content", _content);
        json.WriteBoolean("tts", _tts);

        json.WriteStartArray("embeds");

        foreach (var embedBuilder in _embedBuilders)
            embedBuilder.Build(json);

        json.WriteEndArray();
    }


    public sealed class MentionConfiguration
    {

    }
}
