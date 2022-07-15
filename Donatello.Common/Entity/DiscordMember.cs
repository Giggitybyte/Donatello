namespace Donatello.Entity;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordMember : DiscordUser
{
    private ulong _guildId;
    private JsonElement _member;

    /// <summary></summary>
    public DiscordMember(DiscordApiBot bot, ulong guildId, JsonElement userJson, JsonElement memberJson) : base(bot, userJson)
    {
        _member = memberJson;
        _guildId = guildId;
    }

    /// <summary></summary>
    public DiscordMember(DiscordApiBot bot, ulong guildId, DiscordUser user, JsonElement memberJson) : this(bot, guildId, user.Json, memberJson) { }

    /// <summary></summary>
    public override string AvatarUrl
    {
        get
        {
            if (_member.TryGetProperty("avatar", out var avatarHash) && avatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = avatarHash.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/guilds/{_guildId}/users/{this.Id}/avatars/{avatarHash.GetString()}.{extension}";
            }
            else
                return base.AvatarUrl;
        }
    }

    /// <summary>When the member joined the guild.</summary>
    public DateTimeOffset JoinDate => _member.GetProperty("joined_at").GetDateTimeOffset();

    /// <summary>Whether the member is "server" deafened in voice channels.</summary>
    public bool IsDeafened => _member.GetProperty("deaf").GetBoolean();

    /// <summary>Whether the member is "server" muted in voice channels.</summary>
    public bool IsMuted => _member.GetProperty("mute").GetBoolean();

    /// <summary>Whether the member has not yet met the guild's <see href="https://support.discord.com/hc/en-us/articles/1500000466882">membership screening</see> requirements.</summary>
    /// <remarks>A pending member will not be able to interact with the server until they pass the screening requirements.</remarks>
    public bool IsPending => _member.TryGetProperty("pending", out var property) && property.GetBoolean();

    /// <summary>Returns <see langword="true"/> if the member has a nickname set, <see langword="false"/> otherwise.</summary>
    /// <param name="nickname">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the nickname set by the member,<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasNickname(out string nickname)
    {
        if (_member.TryGetProperty("nick", out var prop) && prop.ValueKind is not JsonValueKind.Null)
            nickname = prop.GetString();
        else
            nickname = string.Empty;

        return nickname != string.Empty;
    }

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public ValueTask<DiscordUser> GetUserAsync()
        => this.Bot.GetUserAsync(this.Id);

    /// <summary></summary>
    public async ValueTask<EntityCollection<DiscordRole>> GetRolesAsync()
    {
        var guild = await GetGuildAsync();
        var roleIds = _member.GetProperty("roles");
        var roles = new Dictionary<DiscordSnowflake, DiscordRole>(roleIds.GetArrayLength());

        foreach (var roleId in roleIds.EnumerateArray())
        {
            var role = await guild.GetRoleAsync(roleId.GetUInt64());
            roles.Add(role.Id, role);
        }

        return new EntityCollection<DiscordRole>(roles);
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
        if (_member.TryGetProperty("premium_since", out var property))
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
        if (_member.TryGetProperty("communication_disabled_until", out var property) && property.ValueKind is not JsonValueKind.Null)
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

