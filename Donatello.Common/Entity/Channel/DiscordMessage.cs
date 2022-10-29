namespace Donatello.Entity;
using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A message sent in a channel.</summary>
public class DiscordMessage : DiscordEntity
{
    public DiscordMessage(DiscordBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    /// <summary>Date when this message was sent.</summary>
    public DateTimeOffset SendDate => this.Json.GetProperty("time").GetDateTimeOffset();

    /// <summary>Returns whether this was a text-to-speech (TTS) message.</summary>
    public bool TextToSpeech => this.Json.GetProperty("tts").GetBoolean();

    /// <summary>Whether this message contains an <c>@everyone</c> string mention.</summary>
    public bool EveryoneMention => this.Json.GetProperty("mention_everyone").GetBoolean();

    /// <summary></summary>
    public ValueTask<DiscordTextChannel> GetChannelAsync()
        => this.Bot.GetChannelAsync<DiscordTextChannel>(this.Json.GetProperty("channel_id").ToSnowflake());

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordUser> GetMentionedUsersAsync()
    {
        var mentionArray = this.Json.GetProperty("mentions");
        if (mentionArray.GetArrayLength() is 0)
            yield break;

        foreach (var userId in mentionArray.EnumerateArray().Select(partialUser => partialUser.GetProperty("id").ToSnowflake()))
        {
            var user = await this.Bot.GetUserAsync(userId);

            if (this.Json.TryGetProperty("guild_id", out var guildProp) && this.Json.TryGetProperty("member", out var memberJson))
                yield return new DiscordGuildMember(this.Bot, guildProp.ToSnowflake(), user, memberJson);
            else
                yield return user;
        }
    }

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildRole> GetMentionedRolesAsync()
    {
        var mentionArray = this.Json.GetProperty("mention_roles");
        var channel = await this.GetChannelAsync();

        if (channel is not DiscordGuildTextChannel guildChannel || mentionArray.GetArrayLength() is 0)
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
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the string value of the message.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasContent(out string content)
    {
        content = this.Json.GetProperty("content").GetString();
        return !string.IsNullOrEmpty(content);
    }

    /// <summary>Returns <see langword="true"/> if this message was edited at any point, <see langword="false"/> otherwise.</summary>
    /// <param name="lastEditDate">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the date when the message was last edited.<br/>
    /// <see langword="false"/> this parameter will be set to <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool WasEdited(out DateTimeOffset lastEditDate)
    {
        var dateJson = this.Json.GetProperty("edited_timestamp");

        if (dateJson.ValueKind is JsonValueKind.Null)
            lastEditDate = DateTimeOffset.MinValue;
        else
            lastEditDate = dateJson.GetDateTimeOffset();

        return lastEditDate != DateTimeOffset.MinValue;
    }

    /// <summary></summary>
    public bool TryGetAuthor(out DiscordUser author)
    {
        if (this.Json.TryGetProperty("webhook_id", out _))
            author = null;
        else
        {
            var partialUserJson = this.Json.GetProperty("author");

            if (this.Json.TryGetProperty("guild_id", out var guildProp) && this.Json.TryGetProperty("member", out var memberJson))
                author = new DiscordGuildMember(this.Bot, guildProp.ToSnowflake(), partialUserJson, memberJson);
            else
                author = new DiscordUser(this.Bot, partialUserJson);
        }

        return author != null;
    }

    /// <summary></summary>
    public bool TryGetWebhook(out ulong webhookId) // Figure out best way to return an entity or merge with above.
    {
        if (this.Json.TryGetProperty("webhook_id", out var webhookProp))
            webhookId = webhookProp.ToSnowflake();
        else
            webhookId = 0;

        return webhookId != 0;
    }
}