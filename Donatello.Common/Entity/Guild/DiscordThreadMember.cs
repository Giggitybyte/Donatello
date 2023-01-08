namespace Donatello.Entity;

using System.Text.Json;
using System.Threading.Tasks;
using System;
using Donatello.Extension.Internal;

/// <summary></summary>
public sealed class DiscordThreadMember : DiscordGuildMember
{
    private readonly JsonElement _threadMember;

    public DiscordThreadMember(DiscordBot bot, DiscordSnowflake guildId, JsonElement userJson, JsonElement guildMemberJson, JsonElement threadMemberJson)
        : base(bot, guildId, userJson, guildMemberJson)
    {
        _threadMember = threadMemberJson;
    }

    public DiscordThreadMember(DiscordBot bot, DiscordGuildMember guildMember, JsonElement threadMemberJson)
        : base(bot, guildMember)
    {
        _threadMember = threadMemberJson;
    }

    /// <summary>Backing thread member object.</summary>
    internal new JsonElement Json => _threadMember;

    /// <inheritdoc cref="DiscordGuild.Member.Json"/>
    internal JsonElement GuildMemberJson => base.Json;

    /// <summary>When the member was added to the thread.</summary>
    public new DateTimeOffset JoinDate => _threadMember.GetProperty("joined_at").GetDateTimeOffset();

    /// <inheritdoc cref="DiscordGuild.Member.JoinDate"/>
    public DateTimeOffset GuildJoinDate => base.JoinDate;

    /// <summary>Fetches the thread this member belongs to.</summary>
    public async ValueTask<DiscordThreadChannel> GetThreadChannelAsync()
    {
        var guild = await this.GetGuildAsync();
        var threadId = _threadMember.GetProperty("id").ToSnowflake();
        var thread = await guild.GetThreadAsync(threadId);

        return thread;
    }
}