﻿namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Rest.Extension.Endpoint;
using Enum;
using Extension;

/// <summary>A sub-channel contained within a guild text channel.</summary>
public class GuildThreadChannel : GuildTextChannel
{
    public GuildThreadChannel(JsonElement json, Bot bot) : base(json, bot) { }
    public GuildThreadChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }

    /// <summary>An additional sub-set of fields sent only with threads.</summary>
    internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

    /// <summary></summary>
    protected internal Snowflake ParentId => this.Json.GetProperty("parent_id").ToSnowflake();

    /// <summary></summary>
    public bool Locked => this.Metadata.GetProperty("locked").GetBoolean();

    /// <summary></summary>
    public bool Archived => this.Metadata.GetProperty("archived").GetBoolean();

    /// <summary></summary>
    public bool Private => this.Type is ChannelType.PrivateThread;

    /// <summary>Whether non-moderators can add other non-moderators to this thread.</summary>
    public bool Invitable => this.Private is false || this.Metadata.GetProperty("invitable").GetBoolean();

    /// <summary></summary>
    public TimeSpan AutoArchiveDuration => TimeSpan.FromMinutes(this.Metadata.GetProperty("auto_archive_duration").GetInt32());

    /// <summary>Current number of messages contained within this thread</summary>
    /// <remarks>Threads created before July 1, 2022 will stop incrementing this value at 50 messages.</remarks>
    public int MessageCount => this.Json.GetProperty("message_count").GetInt32();

    /// <summary>Total number of messages ever sent in this thread.</summary>
    /// <remarks>Threads created before July 1, 2022 will stop incrementing this value at 50 messages.</remarks>
    public int TotalMessageCount => this.Json.GetProperty("total_message_sent").GetInt32();

    /// <summary>Returns <see langword="true"/> if this thread has a creation date field; <see langword="false"/> otherwise.</summary>
    /// <remarks>Only threads created after January 9, 2022 will have a creation date field.</remarks>
    /// <param name="creationDate">If the method returns <see langword="true"/> this parameter will contain the date when this thread was created.
    /// Otherwise, this parameter will be <see cref="DateTimeOffset.MinValue"/>.</param>
    public bool HasCreationDate(out DateTimeOffset creationDate)
        => this.Metadata.TryGetProperty("create_timestamp", out JsonElement prop) & prop.TryGetDateTimeOffset(out creationDate);

    /// <summary>Fetches the user which created this thread.</summary>
    public async Task<GuildMember> GetOwnerAsync()
    {
        var guild = await this.GetGuildAsync();
        var userId = this.Json.GetProperty("owner_id").ToSnowflake();
        var member = await guild.GetMemberAsync(userId);

        return member;
    }

    /// <summary>Fetches the text channel which contains this thread.</summary>
    public ValueTask<GuildTextChannel> GetParentChannelAsync()
    {
        var parentChannelId = this.Json.GetProperty("parent_id").ToSnowflake();
        return this.Bot.GetChannelAsync<GuildTextChannel>(parentChannelId);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<ThreadMember> FetchMembersAsync()
    {
        await foreach (var threadMemberJson in this.Bot.RestClient.GetThreadChannelMembersAsync(this.Id))
        {
            this.Bot.GuildThreadCache[this.GuildId].AddOrUpdate(threadMemberJson);
            var threadMember = new ThreadMember(threadMemberJson, this.Bot);
            yield return threadMember;
        }
    }
}

