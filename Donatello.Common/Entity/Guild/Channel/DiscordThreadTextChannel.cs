namespace Donatello.Entity;

using Donatello.Enum;
using Donatello.Extension.Internal;
using Donatello.Rest.Extension.Endpoint;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A sub-channel contained within a guild text channel.</summary>
public class DiscordThreadTextChannel : DiscordGuildTextChannel
{
    public DiscordThreadTextChannel(DiscordBot bot, JsonElement json)
        : base(bot, json)
    {
        this.MemberCache = new ObjectCache<JsonElement>();
    }

    /// <summary>An additional sub-set of fields sent only with threads.</summary>
    internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

    /// <summary></summary>
    internal ObjectCache<JsonElement> MemberCache { get; init; }

    /// <summary></summary>
    public bool IsLocked => this.Metadata.GetProperty("locked").GetBoolean();

    /// <summary></summary>
    public bool IsArchived => this.Metadata.GetProperty("archived").GetBoolean();

    /// <summary></summary>
    public bool IsPrivate => this.Type is ChannelType.PrivateThread;

    /// <summary>Whether non-moderators can add other non-moderators to this thread.</summary>
    public bool IsInvitable => this.IsPrivate is false || this.Metadata.GetProperty("invitable").GetBoolean();

    /// <summary></summary>
    public TimeSpan AutoArchiveDuration => TimeSpan.FromMinutes(this.Metadata.GetProperty("auto_archive_duration").GetInt32());

    /// <summary>Current number of messages contained within this thread</summary>
    public int MessageCount => this.Json.GetProperty("message_count").GetInt32();

    /// <summary>Total number of messages ever sent in this thread.</summary>
    public int TotalMessageCount => this.Json.GetProperty("total_message_sent").GetInt32();

    /// <summary>Returns <see langword="true"/> if this thread has a creation date field; <see langword="false"/> otherwise.</summary>
    /// <remarks>Only threads created after January 9, 2022 will have a creation date field.</remarks>
    /// <param name="creationDate">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the date when this thread was created.<br/>
    /// <see langword="false"/> this parameter will be <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool HasCreationDate(out DateTimeOffset creationDate)
        => this.Metadata.TryGetProperty("create_timestamp", out JsonElement prop) & prop.TryGetDateTimeOffset(out creationDate);

    /// <summary>Fetches the user which created this thread.</summary>
    public async Task<DiscordGuildMember> GetOwnerAsync()
    {
        var guild = await this.GetGuildAsync();
        var userId = this.Json.GetProperty("owner_id").ToSnowflake();
        var member = await guild.GetMemberAsync(userId);

        return member;
    }

    /// <summary>Fetches the text channel which contains this thread.</summary>
    public ValueTask<DiscordGuildTextChannel> GetParentChannelAsync()
        => this.Bot.GetChannelAsync<DiscordGuildTextChannel>(this.Json.GetProperty("parent_id").ToSnowflake());

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordThreadMember> FetchMembersAsync()
    {
        await foreach (var threadMemberJson in this.Bot.RestClient.GetThreadChannelMembersAsync(this.Id))
        {
            this.MemberCache.Add(threadMemberJson.GetProperty("user_id").ToSnowflake(), threadMemberJson);

            var guild = await this.GetGuildAsync();
            var guildMember = await guild.GetMemberAsync(threadMemberJson.GetProperty("user_id").ToSnowflake());
            var threadMember = new DiscordThreadMember(this.Bot, guildMember, threadMemberJson);

            yield return threadMember;
        }
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<DiscordThreadMember>> GetMembersAsync()
    {
        this.MemberCache.Clear();

        var members = new List<DiscordThreadMember>();
        await foreach (var threadMember in this.FetchMembersAsync())
            members.Add(threadMember);

        return members.AsReadOnly();
    }

}

