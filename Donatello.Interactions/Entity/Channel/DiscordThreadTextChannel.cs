namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Entity.Enumeration;
using Donatello.Interactions.Extension;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Temporary sub-channel inside an existing text channel.</summary>
public sealed class DiscordThreadTextChannel : DiscordGuildTextChannel
{
    internal DiscordThreadTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Thread-specific channel fields that are not used by other channel types.</summary>
    internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

    /// <summary>Whether this is a private thread viewable only by invited (<c>@mentioned</c>) users.</summary>
    public bool IsPrivate => this.Type is ChannelType.PrivateThread;

    /// <summary>Whether the thread has been locked.</summary>
    /// <remarks>When <see langword="true"/>, only users with the <c>MANAGE_THREADS</c> permission will be able to unarchive the thread.</remarks>
    public bool IsLocked => this.Metadata.GetProperty("locked").GetBoolean();

    /// <summary>Whether this thread has been archived.</summary>
    public bool IsArchived => this.Metadata.GetProperty("archived").GetBoolean();

    /// <summary>When the thread's archive status was last changed.</summary>
    public DateTime ArchiveTimestamp => this.Metadata.GetProperty("archive_timestamp").GetDateTime();

    /// <summary>Length of time it'll take for a thread to be automatically archived after the last message was sent.</summary>
    public TimeSpan ArchiveTimeout => TimeSpan.FromMinutes(this.Metadata.GetProperty("auto_archive_duration").GetInt32());

    /// <summary>Fetches the user which started this thread.</summary>
    public Task<DiscordUser> GetCreatorAsync()
    {
        var id = this.Json.GetProperty("owner_id").AsUInt64();
        return this.Bot.GetUserAsync(id);
    }

    /// <summary>Fetches the text channel which contains this thread channel.</summary>
    public async Task<DiscordGuildTextChannel> GetParentChannel()
    {
        var id = this.Json.GetProperty("parent_id").AsUInt64();
        var channel = await this.Bot.GetChannelAsync(id);

        return channel as DiscordGuildTextChannel;
    }
}
