namespace Donatello.Common.Entity.Guild;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Extension;

/// <summary></summary>
public sealed class ThreadMember : GuildMember
{
    private readonly JsonElement _threadMember;

    public ThreadMember(Bot bot, Snowflake guildId, JsonElement userJson, JsonElement guildMemberJson, JsonElement threadMemberJson)
        : base(bot, guildId, userJson, guildMemberJson)
    {
        _threadMember = threadMemberJson;
    }

    public ThreadMember(Bot bot, GuildMember guildMember, JsonElement threadMemberJson)
        : base(bot, guildMember)
    {
        _threadMember = threadMemberJson;
    }

    /// <summary>Backing thread member object.</summary>
    internal new JsonElement Json => _threadMember;

    /// <inheritdoc cref="GuildMember.Json"/>
    internal JsonElement GuildMemberJson => base.Json;

    /// <summary>When the member was added to the thread.</summary>
    public new DateTimeOffset JoinDate => _threadMember.GetProperty("joined_at").GetDateTimeOffset();

    /// <inheritdoc cref="GuildMember.JoinDate"/>
    public DateTimeOffset GuildJoinDate => base.JoinDate;

    /// <summary>Fetches the thread this member belongs to.</summary>
    public async ValueTask<GuildThreadChannel> GetThreadChannelAsync()
    {
        var guild = await this.GetGuildAsync();
        var threadId = _threadMember.GetProperty("id").ToSnowflake();
        var thread = await guild.GetThreadAsync(threadId);

        return thread;
    }
}