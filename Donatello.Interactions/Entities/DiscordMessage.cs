using System;
using System.Text.Json;
using Donatello.Interactions.Extensions;
using Qommon.Collections;

namespace Donatello.Interactions.Entities
{
    /// <summary></summary>
    public sealed class DiscordMessage : DiscordEntity
    {
        public DiscordMessage(JsonElement json) : base(json) { }

        /// <summary>ID of the channel the message was sent in.</summary>
        public ulong ChannelId
            => ulong.Parse(Json.GetProperty("channel_id").GetString());

        /// <summary>User who sent this message.</summary>
        public DiscordUser Author
            => new(Json.GetProperty("author"));

        /// <summary>The contents of this message.</summary>
        public string Content
            => Json.GetProperty("content").GetString();

        /// <summary>Whether the message contains an <c>@everyone</c> mention.</summary>
        public bool MentionsEveryone
            => Json.GetProperty("mention_everyone").GetBoolean();

        /// <summary>A collection of users that were mentioned in this message.</summary>
        public ReadOnlyList<DiscordUser> MentionedUsers
            => new(Json.GetProperty("mentions").ToEntityArray<DiscordUser>());

        /// <summary>A collection of roles that were mentioned in this message.</summary>
        public ReadOnlyList<DiscordRole> MentionedRoles
            => new(Json.GetProperty("mention_roles").ToEntityArray<DiscordRole>());

        /// <summary>When this message was sent.</summary>
        public DateTime Timestamp
            => Json.GetProperty("timestamp").GetDateTime();

        /// <summary>The last date this message was modified.</summary>
        public DateTime? EditTimestamp
            => Json.GetProperty("edited_timestamp").GetDateTime();

        /// <summary>Whether or not the contents of this message have been modified.</summary>
        public bool IsEdited
            => EditTimestamp is not null;
    }
}
