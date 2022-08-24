namespace Donatello.Entity;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordGuildMember : DiscordUser
{
    private ulong _guildId;
    protected JsonElement _guildMember;

    public DiscordGuildMember(DiscordBot bot, DiscordSnowflake guildId, JsonElement userJson, JsonElement memberJson) 
        : base(bot, userJson)
    {
        _guildMember = memberJson;
        _guildId = guildId;
    }

    public DiscordGuildMember(DiscordBot bot, DiscordSnowflake guildId, DiscordUser user, JsonElement memberJson) 
        : this(bot, guildId, user.Json.Clone(), memberJson) { }

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
    public bool IsPending => _guildMember.TryGetProperty("pending", out var property) && property.GetBoolean();

    /// <summary></summary>
    public override string AvatarUrl
    {
        get
        {
            if (_guildMember.TryGetProperty("avatar", out var avatarHash) && avatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = avatarHash.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/guilds/{_guildId}/users/{this.Id}/avatars/{avatarHash.GetString()}.{extension}";
            }
            else
                return base.AvatarUrl;
        }
    }

    /// <summary>Returns <see langword="true"/> if the member has a nickname set, <see langword="false"/> otherwise.</summary>
    /// <param name="nickname">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the nickname set by the member,<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasNickname(out string nickname)
    {
        if (_guildMember.TryGetProperty("nick", out var prop) && prop.ValueKind is not JsonValueKind.Null)
            nickname = prop.GetString();
        else
            nickname = string.Empty;

        return nickname != string.Empty;
    }

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordRole> GetRolesAsync()
    {
        var guild = await this.GetGuildAsync();
        var roleIds = _guildMember.GetProperty("roles");
        var roles = new Dictionary<DiscordSnowflake, DiscordRole>(roleIds.GetArrayLength());

        foreach (var roleId in roleIds.EnumerateArray())
            yield return await guild.GetRoleAsync(roleId.GetUInt64());
    }

    /// <summary>
    /// Returns <see langword="true"/> if the member is 
    /// <see href="https://support.discord.com/hc/en-us/articles/360028038352">boosting</see> 
    /// its guild; <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="startDate">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the date when the member began boosting its guild.<br/>
    /// <see langword="false"/> this parameter will be <see cref="DateTimeOffset.MinValue"/>.
    /// </param>
    public bool IsBooster(out DateTimeOffset startDate)
    {
        if (_guildMember.TryGetProperty("premium_since", out var property))
            startDate = property.GetDateTimeOffset();
        else
            startDate = DateTimeOffset.MinValue;

        return startDate != DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the 
    /// </summary>
    public bool IsCommunicationDisabled(out DateTimeOffset expirationDate)
    {
        if (_guildMember.TryGetProperty("communication_disabled_until", out var property) && property.ValueKind is not JsonValueKind.Null)
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
}

