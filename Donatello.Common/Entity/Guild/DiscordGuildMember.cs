namespace Donatello.Entity;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello;

/// <summary></summary>
public class DiscordGuildMember : DiscordUser, IGuildEntity
{
    private readonly ulong _guildId;
    private readonly JsonElement _guildMember;

    public DiscordGuildMember(DiscordBot bot, DiscordSnowflake guildId, JsonElement userJson, JsonElement memberJson)
        : base(bot, userJson)
    {
        _guildMember = memberJson;
        _guildId = guildId;
    }

    public DiscordGuildMember(DiscordBot bot, DiscordSnowflake guildId, DiscordUser user, JsonElement memberJson)
        : this(bot, guildId, user.Json, memberJson)
    {

    }

    protected DiscordGuildMember(DiscordBot bot, DiscordGuildMember member)
        : this(bot, member._guildId, member.UserJson, member._guildMember)
    {

    }

    /// <summary>Backing guild member object.</summary>
    protected internal new JsonElement Json => _guildMember;

    /// <summary></summary>
    protected internal JsonElement UserJson => base.Json;

    /// <summary>When the member joined the guild.</summary>
    public DateTimeOffset JoinDate => _guildMember.GetProperty("joined_at").GetDateTimeOffset();

    /// <summary>Whether the member is deafened in guild voice channels.</summary>
    public bool IsDeafened => _guildMember.GetProperty("deaf").GetBoolean();

    /// <summary>Whether the member is muted in guild voice channels.</summary>
    public bool IsMuted => _guildMember.GetProperty("mute").GetBoolean();

    /// <summary>Whether the member has not yet met the guild's <see href="https://support.discord.com/hc/en-us/articles/1500000466882">membership screening</see> requirements.</summary>
    /// <remarks>A pending member will not be able to interact with the guild until they pass the screening requirements.</remarks>
    public bool IsPending => _guildMember.TryGetProperty("pending", out JsonElement property) && property.GetBoolean();

    /// <summary>Member avatar URL.</summary>
    public override string AvatarUrl
    {
        get
        {
            if (_guildMember.TryGetProperty("avatar", out JsonElement avatarHash) && avatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = avatarHash.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/guilds/{_guildId}/users/{this.Id}/avatars/{avatarHash.GetString()}.{extension}";
            }
            else
                return base.AvatarUrl;
        }
    }

    /// <summary>Returns <see langword="true"/> if the member has a nickname set, <see langword="false"/> otherwise.</summary>
    /// <param name="nickname">When the method returns <see langword="true"/> this parameter will contain the nickname set by the member; otherwise it'll contain an empty string.</param>
    public bool HasNickname(out string nickname)
    {
        nickname = _guildMember.TryGetProperty("nick", out JsonElement prop) && prop.ValueKind is not JsonValueKind.Null
            ? prop.GetString()
            : string.Empty;

        return nickname != string.Empty;
    }

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildRole> GetRolesAsync()
    {
        var guild = await this.GetGuildAsync();

        foreach (var roleId in _guildMember.GetProperty("roles").EnumerateArray())
            yield return await guild.GetRoleAsync(roleId.GetUInt64());
    }

    /// <summary>Returns <see langword="true"/> if the member is <see href="https://support.discord.com/hc/en-us/articles/360028038352">boosting</see> its guild; <see langword="false"/> otherwise.</summary>
    /// <param name="startDate"> When the method returns <see langword="true"/> this parameter will contain the date when the member began boosting its guild; otherwise it'll be <see cref="DateTimeOffset.MinValue"/>.</param>
    public bool IsBooster(out DateTimeOffset startDate)
    {
        startDate = _guildMember.TryGetProperty("premium_since", out JsonElement property)
            ? property.GetDateTimeOffset()
            : DateTimeOffset.MinValue;

        return startDate != DateTimeOffset.MinValue;
    }

    /// <summary>Returns <see langword="true"/> if a guild moderator has restricted this member from interacting with the guild.</summary>
    /// <param name="expirationDate">When the method returns <see langword="true"/> this parameter will contain date when the member will be allowed to interact with the guild; otherwise it'll be <see cref="DateTimeOffset.MinValue"/></param>
    public bool IsCommunicationDisabled(out DateTimeOffset expirationDate)
    {
        if (_guildMember.TryGetProperty("communication_disabled_until", out JsonElement property) && property.ValueKind is not JsonValueKind.Null)
        {
            var date = property.GetDateTimeOffset();
            if (DateTimeOffset.UtcNow < date)
            {
                expirationDate = date;
                return true;
            }
        }

        expirationDate = DateTimeOffset.MinValue;
        return false;
    }

    public class Presence
    {
        /// <summary></summary>
        public Status Status { get; internal init; }


    }
}

