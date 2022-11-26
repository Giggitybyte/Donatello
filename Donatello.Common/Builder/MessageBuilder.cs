namespace Donatello.Builder;

using Donatello.Entity;
using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

/// <summary></summary>
public sealed class MessageBuilder : EntityBuilder<DiscordMessage>
{
    /// <summary>Returns the combined total number of characters from message content and embeds.</summary>
    /// <remarks>
    /// Discord limits messages to a total of 6,000 characters shared between the content of the message and each of its embeds.<br/>
    /// This method can help you determine when to split a message with long content or many embeds into multiple messages.
    /// </remarks>
    public ushort GetCharacterCount()
    {

    }

    /// <summary></summary>
    public MessageBuilder SetContent(string content)
    {
        if (content.Length <= 2000)
            this.Json["content"] = content;
        else
            throw new ArgumentException("Content cannot be greater than 2,000 characters");

        return this;
    }

    /// <summary>Adds a reference to an existing message.</summary>
    public MessageBuilder SetReply(DiscordMessage message)
    {
        this.Json["message_reference"] = new JsonObject()
        {
            ["message_id"] = message.Id.Value,
            ["channel_id"] = message.Value
        };

        return this;
    }

    /// <summary>Whether the message should be spoken aloud in the client using text-to-speech (TTS).</summary>
    public MessageBuilder SetTextToSpeech(bool value)
    {
        this.Json["tts"] = value;
        return this;
    }

    /// <summary>Add an embed to the message.</summary>
    public MessageBuilder AppendEmbed(Action<EmbedBuilder> embedDelegate)
    {
        var builder = new EmbedBuilder();
        embedDelegate(builder);

        return this.AppendEmbed(builder);
    }

    /// <summary></summary>
    public MessageBuilder AppendEmbed(EmbedBuilder embedBuilder)
    {
        var embedNode = this.Json["embeds"] ??= new JsonArray();
        var embedArray = embedNode.AsArray();

        if (embedArray.Count + 1 <= 10)
            embedArray.Add(embedBuilder.Json);
        else
            throw new InvalidOperationException("Message cannot have more than 10 embeds.");

        return this;
    }

    /// <summary></summary>
    public void ClearEmbeds()
        => this.Json["embeds"]?.AsArray()?.Clear();

    /// <summary></summary>
    public MessageBuilder ConfigureMentions(Action<MentionConfig> config)
    {
        _mentionConfiguration ??= new MentionConfig();
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public class MentionConfig
    {
        public MentionConfig()
        {
            this.UserIds = new List<DiscordSnowflake>();
            this.RoleIds = new List<DiscordSnowflake>();
            this.AllowedTypes = MentionType.None;
        }

        /// <summary></summary>
        internal List<DiscordSnowflake> RoleIds { get; private init; }

        /// <summary></summary>
        internal List<DiscordSnowflake> UserIds { get; private init; }

        /// <summary></summary>
        public MentionType AllowedTypes { get; set; }
    }

    /// <summary></summary>
    [Flags]
    public enum MentionType
    {
        None = 0,
        Roles = 1,
        Users = 2,
        Everyone = 4
    }
}
