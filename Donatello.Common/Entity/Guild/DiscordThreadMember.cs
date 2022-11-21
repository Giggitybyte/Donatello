namespace Donatello.Entity;

using Donatello;
using Donatello.Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class DiscordThreadMember : DiscordGuildMember
{
    private JsonElement _threadMember;

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
    protected internal new JsonElement Json => _threadMember;

    /// <inheritdoc cref="DiscordGuildMember.Json"/>
    protected internal JsonElement GuildMemberJson => base.Json;

    /// <summary>When the member was added to the thread.</summary>
    public new DateTimeOffset JoinDate => _threadMember.GetProperty("joined_at").GetDateTimeOffset();

    /// <inheritdoc cref="DiscordGuildMember.JoinDate"/>
    public DateTimeOffset GuildJoinDate => base.JoinDate;

    /// <summary></summary>
    public async ValueTask<DiscordThreadTextChannel> GetThreadChannelAsync()
    {
        var guild = await this.GetGuildAsync();
        var threadId = _threadMember.GetProperty("id").ToSnowflake();
        var thread = await guild.GetThreadAsync(threadId);

        return thread;
    }
}