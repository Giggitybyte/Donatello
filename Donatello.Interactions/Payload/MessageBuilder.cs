namespace Donatello.Interactions.Payload;

using Donatello.Interactions.Entity;
using Donatello.Rest.Transport;
using System;
using System.Collections.Generic;
using System.Text.Json;

public sealed class MessageBuilder : PayloadWriter
{
    private DiscordMessage _messageReference;
    private MentionConfiguration _mentionConfiguration;
    private List<string> _stickerIds;
    private List<EmbedBuilder> embeds;
    private string _content;
    private bool _tts;

    internal MessageBuilder()
    {
        embeds = new List<EmbedBuilder>(10);
        this.Attachments = new List<FileAttachment>(10);
    }

    /// <summary></summary>
    internal List<FileAttachment> Attachments { get; private init; }

    internal override void WritePayload(Utf8JsonWriter json)
    {
        if (this.Attachments.Count is 0 & embeds.Count is 0 & _content is null & _stickerIds.Count is 0)
            throw new Exception();

        json.WriteString("content", _content);
        json.WriteBoolean("tts", _tts);

        json.WriteStartArray("embeds");

        foreach (var embedBuilder in embeds)
            embedBuilder.WritePayload(json);

        json.WriteEndArray();
    }

    /// <summary></summary>
    public MessageBuilder SetContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot be greater than 2,000 characters", nameof(content));

        _content = content;
        return this;
    }

    /// <summary>Whether the message should be spoken aloud in the client using text-to-speech.</summary>
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
        if (embeds.Count + 1 > embeds.Capacity)
            throw new InvalidOperationException($"Message cannot have more than {embeds.Capacity} embeds.");

        var builder = new EmbedBuilder();
        embed(builder);
        embeds.Add(builder);

        return this;
    }

    /// <summary></summary>
    public MessageBuilder ConfigureMentions(Action<MentionConfiguration> config)
    {
        _mentionConfiguration ??= new MentionConfiguration();
        throw new NotImplementedException();
    }

    public sealed class MentionConfiguration
    {

    }
}
