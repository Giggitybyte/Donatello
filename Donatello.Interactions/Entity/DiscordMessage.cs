namespace Donatello.Interactions.Entity;

using System;
using System.Text.Json;
using Donatello.Interactions;
using Qommon.Collections;

/// <summary></summary>
public sealed class DiscordMessage : DiscordEntity
{
    public DiscordMessage(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>ID of the channel the message was sent in.</summary>
    public ulong ChannelId => ulong.Parse(this.Json.GetProperty("channel_id").GetString());

    /// <summary>User who sent this message.</summary>
    public DiscordUser Author => new(this.Bot, this.Json.GetProperty("author"));

    /// <summary>The contents of this message.</summary>
    public string Content => this.Json.GetProperty("content").GetString();

    /// <summary>Whether the message contains an <c>@everyone</c> mention.</summary>
    public bool MentionsEveryone => this.Json.GetProperty("mention_everyone").GetBoolean();

    /// <summary>A collection of users that were mentioned in this message.</summary>
    public ReadOnlyList<DiscordUser> MentionedUsers => new(this.Json.GetProperty("mentions").ToEntityArray<DiscordUser>(this.Bot));

    /// <summary>A collection of roles that were mentioned in this message.</summary>
    public ReadOnlyList<DiscordRole> MentionedRoles => new(this.Json.GetProperty("mention_roles").ToEntityArray<DiscordRole>(this.Bot));

    /// <summary>When this message was sent.</summary>
    public DateTime Timestamp => this.Json.GetProperty("timestamp").GetDateTime();

    /// <summary>The last date this message was modified.</summary>
    public DateTime? EditTimestamp => this.Json.GetProperty("edited_timestamp").GetDateTime();

    /// <summary>Whether or not the contents of this message have been modified.</summary>
    public bool IsEdited => this.EditTimestamp is not null;
}
