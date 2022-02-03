namespace Donatello.Interactions.Writer;

using Donatello.Interactions.Entity;
using Donatello.Rest.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

public sealed class MessageWriter : PayloadWriter
{
    private string _content;
    private bool _tts;
    private List<EmbedWriter> _embeds;
    private List<string> _stickerIds;
    private DiscordMessage _messageReference;
    private MentionConfiguration _mentionConfiguration;

    internal MessageWriter()
    {
        _embeds = new List<EmbedWriter>(10);
    }

    internal override void WritePayload(Utf8JsonWriter json)
    {
        if (_embeds.Count is 0 && _attachments.Count is 0 && _content is null && _stickerIds.Count is 0)
            throw new FormatException("A message requires an embed, file, sticker, or text content.");

        json.WriteString("content", _content);
        json.WriteBoolean("tts", _tts);

        json.WriteStartArray("embeds");

        foreach (var embedBuilder in _embeds)
            embedBuilder.WritePayload(json);

        json.WriteEndArray();
    }

    /// <summary>Add an embed to the message.</summary>
    public MessageWriter AppendEmbed(Action<EmbedWriter> embed)
    {
        if (_embeds.Count + 1 > _embeds.Capacity)
            throw new InvalidOperationException($"Message cannot have more than {_embeds.Capacity} embeds.");

        var builder = new EmbedWriter();
        embed(builder);
        _embeds.Add(builder);

        return this;
    }

    /// <summary></summary>
    public MessageWriter SetContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot be greater than 2,000 characters", nameof(content));

        _content = content;
        return this;
    }

    /// <summary>Whether the message should be spoken aloud in the client using text-to-speech.</summary>
    public MessageWriter SetTts(bool value)
    {
        _tts = value;
        return this;
    }

    /// <summary></summary>
    public MessageWriter SetReply(DiscordMessage message)
    {
        _messageReference = message;
        return this;
    }

    /// <summary></summary>
    public MessageWriter ConfigureMentions(Action<MentionConfiguration> config)
    {
        _mentionConfiguration ??= new MentionConfiguration();
        throw new NotImplementedException();
    }

    public sealed class MentionConfiguration
    {

    }
}
