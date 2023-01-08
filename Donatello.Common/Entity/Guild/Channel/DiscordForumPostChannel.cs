namespace Donatello.Entity;

using Donatello.Cache;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A post</summary>
internal class DiscordForumPostChannel : DiscordGuildTextChannel
{
    public DiscordForumPostChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json)
    {

    }

    /// <summary></summary>
    internal JsonCache MemberCache { get; init; }

    /// <summary>An additional sub-set of fields sent only with threads.</summary>
    internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

    /// <summary>Whether this topic has been locked by a moderator.</summary>
    /// <remarks>Users will be unable to create replies to a locked topic.</remarks>
    public bool Locked => this.Metadata.GetProperty("locked").GetBoolean();

    /// <summary>Whether this topic has been closed and archived.</summary>
    /// <remarks>Users can reopen topics at any time by creating a new post.</remarks>
    public bool Closed => this.Metadata.GetProperty("archived").GetBoolean();

    /// <summary>The date when this topic was posted.</summary>
    public DateTimeOffset CreationDate => this.Metadata.GetProperty("create_timestamp").GetDateTimeOffset();

    /// <summary>Fetches the forum which contains this topic.</summary>
    public ValueTask<DiscordForumChannel> GetForumAsync()
    {

    }
}
