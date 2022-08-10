namespace Donatello.Entity.Builder;

using Donatello.Entity;
using Donatello.Rest;
using System;
using System.Collections.Generic;
using System.Text.Json;

public sealed class MessageBuilder : EntityBuilder
{
    public sealed record MentionConfiguration(); // TODO

    private string _content;
    private bool _tts;
    private List<Attachment> _attachments;
    private List<EmbedBuilder> _embeds;
    private List<string> _stickerIds;
    private DiscordMessage _messageReference;
    private MentionConfiguration _mentionConfiguration;

    /// <summary></summary>
    public MessageBuilder()
    {
        _embeds = new List<EmbedBuilder>(10);
    }

    /// <summary>Add an embed to the message.</summary>
    public MessageBuilder AppendEmbed(Action<EmbedBuilder> embedDelegate)
    {
        if (_embeds.Count + 1 > _embeds.Capacity)
            throw new InvalidOperationException($"Message cannot have more than {_embeds.Capacity} embeds.");

        var builder = new EmbedBuilder();
        embedDelegate(builder);
        _embeds.Add(builder);

        return this;
    }

    /// <summary></summary>
    public MessageBuilder SetContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot be greater than 2,000 characters", nameof(content));

        _content = content;
        return this;
    }

    /// <summary>Whether the message should be spoken aloud in the client using text-to-speech (TTS).</summary>
    public MessageBuilder SetTextToSpeech(bool value)
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

    /// <summary></summary>
    public MessageBuilder ConfigureMentions(Action<MentionConfiguration> config)
    {
        _mentionConfiguration ??= new MentionConfiguration();
        throw new NotImplementedException();
    }

    internal override void ConstructJson(in Utf8JsonWriter jsonWriter)
    {
        if (_embeds.Count is 0 && _attachments.Count is 0 && _content is null && _stickerIds.Count is 0)
            throw new FormatException("A message must have an embed, file, sticker, or text.");

        jsonWriter.WriteString("content", _content);
        jsonWriter.WriteBoolean("tts", _tts);

        jsonWriter.WriteStartArray("embeds");

        foreach (var embedBuilder in _embeds)
            embedBuilder.ConstructJson(jsonWriter);

        jsonWriter.WriteEndArray();
    }
}
