namespace Donatello.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;

public class DiscordThreadMember : DiscordGuildMember
{
    protected JsonElement _threadMember;

    public DiscordThreadMember(DiscordBot bot, DiscordSnowflake guildId, JsonElement userJson, JsonElement guildMemberJson, JsonElement threadMemberJson)
        : base(bot, guildId, userJson, guildMemberJson)
    {
        _threadMember = threadMemberJson;
    }

    public DiscordThreadMember(DiscordBot bot, DiscordSnowflake guildId, DiscordGuildMember guildMember, JsonElement threadMemberJson)
        : this(bot, guildId, guildMember.UserJson, guildMember.Json, threadMemberJson) { }

    /// <summary></summary>
    protected internal new JsonElement Json => _threadMember;

    /// <inheritdoc cref="DiscordGuildMember.Json"/>
    protected internal JsonElement GuildMemberJson => base.Json;

    /// <summary>When the member was added to the thread.</summary>
    public new DateTimeOffset JoinDate => _threadMember.GetProperty("joined_at").GetDateTimeOffset();

    /// <inheritdoc cref="DiscordGuildMember.JoinDate"/>
    public DateTimeOffset GuildJoinDate => base.JoinDate;

    public async ValueTask<DiscordThreadTextChannel> GetThreadChannelAsync()
    {
        var guild = await this.GetGuildAsync();
        return await guild.thr
    }
}