namespace Donatello.Builder;

using Donatello.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

/// <summary></summary>
public sealed class MessageBuilder : EntityBuilder
{
    [Flags]
    public enum MentionType
    {
        Default = 0,

        /// <summary>Do not parse any mentions from message content.</summary>
        None = 1,

        /// <summary>Parse role mentions from message content.</summary>
        Roles = 2,

        /// <summary>Parse user mentions from message content.</summary>
        Users = 4,

        /// <summary>Parse <c>@everyone</c> mentions from message content.</summary>
        Everyone = 8
    }

    public sealed class AllowedMentions
    {
        public AllowedMentions()
        {
            this.UserIds = new List<Snowflake>();
            this.RoleIds = new List<Snowflake>();
            this.RepliedUser = false;
            this.Types = MentionType.Default;
        }

        /// <summary></summary>
        public List<Snowflake> RoleIds { get; private init; }

        /// <summary></summary>
        public List<Snowflake> UserIds { get; private init; }

        /// <summary>Whether to mention the author of a message being replied to</summary>
        public bool RepliedUser { get; private set; }

        /// <summary></summary>
        public MentionType Types { get; set; }
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

    /// <summary></summary>
    public MessageBuilder SetAllowedMentions(AllowedMentions allowedMentions)
    {
        if (allowedMentions.Types.HasFlag(MentionType.Users) && allowedMentions.UserIds.Any())
            throw new InvalidOperationException("User mention parse type cannot be specified when user IDs are provided.");
        else if (allowedMentions.Types.HasFlag(MentionType.Roles) && allowedMentions.RoleIds.Any())
            throw new InvalidOperationException("Role mention parse type cannot be specified when role IDs are provided.");
        else if (allowedMentions.UserIds.Count >= 100)
            throw new InvalidOperationException("Count of user IDs cannot be greater than 100.");
        else if (allowedMentions.RoleIds.Count >= 100)
            throw new InvalidOperationException("Count of role IDs cannot be greater than 100.");

        var mentionJson = new JsonObject();

        if (allowedMentions.Types is not MentionType.Default)
        {
            var typeArray = new JsonArray();

            if (allowedMentions.Types.HasFlag(MentionType.None) is false)
            {
                foreach (var flag in Enum.GetValues(typeof(MentionType)).Cast<MentionType>().Skip(2))
                    if (allowedMentions.Types.HasFlag(flag))
                        typeArray.Add(flag.ToString().ToLower());
            }

            mentionJson["parse"] = typeArray;
        }

        if (allowedMentions.UserIds.Any())
        {
            var userArray = new JsonArray();

            foreach (Snowflake snowflake in allowedMentions.UserIds)
                userArray.Add(snowflake);

            mentionJson["users"] = userArray;
        }

        if (allowedMentions.RoleIds.Any())
        {
            var roleArray = new JsonArray();

            foreach (Snowflake snowflake in allowedMentions.RoleIds)
                roleArray.Add(snowflake);

            mentionJson["roles"] = roleArray;
        }

        if (allowedMentions.RepliedUser)
            mentionJson["replied_user"] = true;

        this.Json["allowed_mentions"] = mentionJson;
        return this;
    }

    /// <summary></summary>
    public MessageBuilder SetAllowedMentions(Action<AllowedMentions> mentionDelegate)
    {
        var allowedMentions = new AllowedMentions();
        mentionDelegate(allowedMentions);

        return this.SetAllowedMentions(allowedMentions);
    }

    /// <summary>Adds a reference to an existing message.</summary>
    public MessageBuilder SetReply(Message message)
    {
        this.Json["message_reference"] = new JsonObject()
        {
            ["message_id"] = message.Id.Value,
            ["channel_id"] = message.ChannelId.Value,
        };

        return this;
    }

    /// <summary>Whether the message should be spoken aloud by a user's Discord client using text-to-speech (TTS).</summary>
    public MessageBuilder SetTextToSpeech(bool value)
    {
        this.Json["tts"] = value;
        return this;
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

    /// <summary>Add an embed to the message.</summary>
    public MessageBuilder AppendEmbed(Action<EmbedBuilder> embedDelegate)
    {
        var builder = new EmbedBuilder();
        embedDelegate(builder);

        return this.AppendEmbed(builder);
    }

    /// <summary>Removes </summary>
    public void ClearEmbeds()
        => this.Json.Remove("embeds");
}
