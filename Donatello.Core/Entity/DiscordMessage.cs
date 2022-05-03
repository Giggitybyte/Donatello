﻿namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A message sent in a channel within Discord.</summary>
public sealed class DiscordMessage : DiscordEntity
{
    public DiscordMessage(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    /// <summary>Date when this message was sent.</summary>
    public DateTimeOffset SendDate => this.Json.GetProperty("time").GetDateTimeOffset();

    /// <summary>Returns whether this was a text-to-speech (TTS) message.</summary>
    public bool IsTts => this.Json.GetProperty("tts").GetBoolean();

    /// <summary>Whether this message contains an <c>@everyone</c> string mention.</summary>
    public bool HasEveryoneMention => this.Json.GetProperty("mention_everyone").GetBoolean();

    /// <summary></summary>
    public ValueTask<DiscordTextChannel> GetChannelAsync()
        => this.Bot.GetChannelAsync<DiscordTextChannel>(this.Json.GetProperty("channel_id").ToUInt64());

    /// <summary></summary>
    public async ValueTask<EntityCollection<DiscordUser>> GetMentionedUsersAsync()
    {
        var mentionedUsers = this.Json.GetProperty("mentions");

        if (mentionedUsers.GetArrayLength() is 0)
            return EntityCollection<DiscordUser>.Empty;

        var userDictionary = new Dictionary<ulong, DiscordUser>();
        foreach (var partialUser in mentionedUsers.EnumerateArray())
        {
            var userJson = await this.Bot.GetUserJsonAsync(partialUser.GetProperty("id").ToUInt64());

            if (this.Json.TryGetProperty("guild_id", out var guildProp) && this.Json.TryGetProperty("member", out var memberJson))
            {
                var member = new DiscordMember(this.Bot, guildProp.ToUInt64(), userJson, memberJson);
                userDictionary.Add(member.Id, member);
            }
            else
            {
                var user = new DiscordUser(this.Bot, userJson);
                userDictionary.Add(user.Id, user);
            }
        }

        return new EntityCollection<DiscordUser>(userDictionary);
    }

    public async ValueTask<EntityCollection<DiscordRole>> GetMentionedRolesAsync()
    {
        var mentionedRoles = this.Json.GetProperty("mention_roles");

        if (mentionedRoles.GetArrayLength() is 0)
            return EntityCollection<DiscordRole>.Empty;

        

        var roleDictionary = new Dictionary<ulong, DiscordRole>();
        foreach (var role in mentionedRoles.EnumerateArray())
        {
            
        }
    }

    /// <summary>Returns <see langword="true"/> if this message had any <see cref="string"/> content sent, <see langword="false"/> otherwise.</summary>
    /// <param name="content">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the string value of the message.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasContent(out string content)
    {
        content = this.Json.GetProperty("content").GetString();
        return string.IsNullOrEmpty(content);
    }

    /// <summary>Returns <see langword="true"/> if this message was edited at any point, <see langword="false"/> otherwise.</summary>
    /// <param name="lastEditDate">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the date when the message was last edited.<br/>
    /// <see langword="false"/> this parameter will be set to <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool WasEdited(out DateTimeOffset lastEditDate)
    {
        var editedProp = this.Json.GetProperty("edited_timestamp");

        if (editedProp.ValueKind is JsonValueKind.Null)
        {
            lastEditDate = DateTimeOffset.MinValue;
            return false;
        }
        else
        {
            lastEditDate = editedProp.GetDateTimeOffset();
            return true;
        }
    }

    /// <summary></summary>
    public bool TryGetAuthor(out DiscordUser author)
    {
        author = null;

        if (this.Json.TryGetProperty("webhook_id", out _))
            return false;

        var partialUserJson = this.Json.GetProperty("author"); // This is a partial user; figure out best way to fetch from cache.
        var userJson = await this.Bot.GetUserJsonAsync(partialUser.GetProperty("id").ToUInt64());

        if (this.Json.TryGetProperty("guild_id", out var guildProp) && this.Json.TryGetProperty("member", out var memberJson))
            author = new DiscordMember(this.Bot, guildProp.ToUInt64(), userJson, memberJson);
        else
            author = new DiscordUser(this.Bot, userJson);

        return author != null;
    }

    /// <summary></summary>
    public bool TryGetWebhook(out ulong webhookId) // Figure out best way to return an entity or merge with above.
    {
        if (this.Json.TryGetProperty("webhook_id", out var webhookProp))
            webhookId = webhookProp.ToUInt64();
        else
            webhookId = 0;

        return webhookId != 0;
    }
}