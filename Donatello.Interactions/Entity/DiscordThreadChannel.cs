﻿namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Entity.Enumeration;
using Donatello.Interactions.Extension;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Temporary sub-channel inside an existing text channel.</summary>
public sealed class DiscordThreadChannel : DiscordGuildTextChannel
{
    internal DiscordThreadChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Thread-specific channel fields that are not used by other channel types.</summary>
    internal JsonElement Metadata { get => this.Json.GetProperty("thread_metadata"); }

    /// <summary>Whether this is a private invite-only thread.</summary>
    public bool IsPrivate { get => this.Type is ChannelType.PrivateThread; }

    /// <summary>Whether the thread has been locked.</summary>
    /// <remarks>When <see langword="true"/>, only users with the <c>MANAGE_THREADS</c> permission will be able to unarchive the thread.</remarks>
    public bool IsLocked { get => this.Metadata.GetProperty("locked").GetBoolean(); }

    /// <summary>Whether this thread has been archived.</summary>
    public bool IsArchived { get => this.Metadata.GetProperty("archived").GetBoolean(); }

    /// <summary>When the thread's archive status was last changed.</summary>
    public DateTime ArchiveTimestamp { get => this.Metadata.GetProperty("archive_timestamp").GetDateTime(); }

    /// <summary>Length of time it'll take for a thread to be automatically archived after recent activity.</summary>
    public TimeSpan ArchiveTimeout { get => TimeSpan.FromMinutes(this.Metadata.GetProperty("auto_archive_duration").GetInt32()); }

    /// <summary>Fetches the user which started this thread.</summary>
    public async Task<DiscordUser> GetCreatorAsync()
    {
        var id = this.Json.GetProperty("owner_id").AsUInt64();
        return await this.Bot.GetUserAsync(id);
    }

    public async Task<DiscordGuildTextChannel> GetParentChannel()
    {
        var id = this.Json.GetProperty("parent_id").AsUInt64();
        return await this.Bot.GetChannelAsync(id);
    }
}
