namespace Donatello.Entity;

using Donatello.Enumeration;
using Donatello.Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordThreadTextChannel : DiscordTextChannel
{
    public DiscordThreadTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

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
    public uint MessageCount => this.Json.GetProperty("message_count").GetUInt32();

    /// <summary>Total number of messages ever sent in this thread.</summary>
    public uint TotalMessageCount => this.Json.GetProperty("total_message_sent").GetUInt32();

    /// <summary>Returns <see langword="true"/> if this thread has a creation date field; <see langword="false"/> otherwise.</summary>
    /// <remarks>Only threads created after January 9, 2022 will have a creation date field.</remarks>
    /// <param name="creationDate">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the date when this thread was created.<br/>
    /// <see langword="false"/> this parameter will be <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool HasCreationDate(out DateTimeOffset creationDate)
        => this.Metadata.TryGetProperty("create_timestamp", out JsonElement prop) & prop.TryGetDateTimeOffset(out creationDate);

    /// <summary>Fetches the guild which contains the parent channel of this thread.</summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.Json.GetProperty("guild_id").ToSnowflake());

    /// <summary>Fetches the user which created this thread.</summary>
    public async ValueTask<DiscordMember> GetOwnerAsync()
    {
        var guild = await GetGuildAsync();
        var member = await guild.GetMemberAsync(this.Json.GetProperty("owner_id").ToSnowflake());

        return member;
    }

    /// <summary>Fetches the channel which contains this thread.</summary>
    public ValueTask<DiscordGuildTextChannel> GetParentChannelAsync()
        => this.Bot.GetChannelAsync<DiscordGuildTextChannel>(this.Json.GetProperty("parent_id").ToSnowflake());
}

