namespace Donatello.Common.Entity.Message;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Extension;
using Guild;
using Guild.Channel;
using User;

/// <summary>A message sent in a channel.</summary>
public sealed partial class Message : Entity
{
    public Message(JsonElement jsonObject, Bot bot)
        : base(jsonObject, bot)
    {

    }

    /// <summary></summary>
    public Snowflake ChannelId => this.Json.GetProperty("channel_id").ToSnowflake();

    /// <summary>Date when this message was sent.</summary>
    public DateTimeOffset SendDate => this.Json.GetProperty("timestamp").GetDateTimeOffset();

    /// <summary>Returns whether this was a text-to-speech (TTS) message.</summary>
    public bool TextToSpeech => this.Json.GetProperty("tts").GetBoolean();

    /// <summary>Whether this message contains an <c>@everyone</c> string mention.</summary>
    public bool MentionEveryone => this.Json.GetProperty("mention_everyone").GetBoolean();

    /// <summary></summary>
    public ValueTask<IChannel> GetChannelAsync()
        => this.Bot.GetChannelAsync<IChannel>(this.ChannelId);

    /// <summary></summary>
    public IEnumerable<User> GetMentionedUsers()
    {
        var mentions = this.Json.GetProperty("mentions");
        if (mentions.GetArrayLength() is 0) yield break;
        
        foreach (var partialUser in mentions.EnumerateArray())
            yield return new User(partialUser, this.Bot);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<Role> GetMentionedRolesAsync()
    {
        var channel = await this.Bot.GetChannelAsync<ITextChannel>(this.ChannelId);

        if (channel is not GuildTextChannel guildChannel)
            throw new InvalidOperationException("Channel must be a guild text channel.");

        if (this.Json.TryGetProperty("mention_roles", out JsonElement mentionArray) is false || mentionArray.GetArrayLength() is 0)
            yield break;

        foreach (var roleId in mentionArray.EnumerateArray().Select(json => json.ToSnowflake()))
        {
            var guild = await guildChannel.GetGuildAsync();
            var role = await guild.GetRoleAsync(roleId);

            yield return role;
        }
    }

    /// <summary>Returns <see langword="true"/> if this message contained any <see cref="string"/> content, <see langword="false"/> otherwise.</summary>
    /// <param name="content">
    /// If the method returns <see langword="true"/> this parameter will contain the string value of the message,
    /// otherwise it will contain an empty string.
    /// </param>
    public bool HasContent(out string content)
    {
        content = this.Json.GetProperty("content").GetString();
        return !string.IsNullOrEmpty(content);
    }

    /// <summary>Returns <see langword="true"/> if this message was edited at any point in time, <see langword="false"/> otherwise.</summary>
    /// <param name="lastEditDate">
    /// If the method returns <see langword="true"/> this parameter will contain the date when the message was last edited,
    /// otherwise it will be <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool WasEdited(out DateTimeOffset lastEditDate)
    {
        var dateJson = this.Json.GetProperty("edited_timestamp");

        lastEditDate = dateJson.ValueKind is not JsonValueKind.Null 
            ? dateJson.GetDateTimeOffset() 
            : DateTimeOffset.MinValue;

        return lastEditDate != DateTimeOffset.MinValue;
    }

    /// <summary></summary>
    public bool TryGetAuthor(out User author)
    {
        author = this.Json.TryGetProperty("webhook_id", out _) 
            ? null // Figure out how to represent a webhook
            : new User(this.Json.GetProperty("author"), this.Bot);

        return author != null;
    }

    /// <summary></summary>
    public bool TryGetWebhook(out ulong webhookId) // Figure out best way to return an entity or merge with above.
    {
        webhookId = this.Json.TryGetProperty("webhook_id", out var webhookProp) 
            ? webhookProp.ToSnowflake() 
            : 0ul;

        return webhookId != 0;
    }
}