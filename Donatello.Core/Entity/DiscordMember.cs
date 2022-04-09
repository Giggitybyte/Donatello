namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordMember : DiscordEntity
{
    private ulong _userId, _guildId;

    internal DiscordMember(DiscordApiBot bot, JsonElement json, ulong guildId) : base(bot, json)
    {
        if (this.Json.TryGetProperty("user", out var property))
            _userId = property.GetProperty("id").ToUInt64();
        else
            throw new ArgumentException("Provided JSON object does not contain a user field.", nameof(json));

        _guildId = guildId;
    }

    /// <summary>Unique snowflake identifier for the user that this object represents.</summary>
    public override ulong Id => _userId;

    /// <summary></summary>
    public string Nickname => this.Json.TryGetProperty("nick", out var property) ? property.GetString() : string.Empty;

    /// <summary></summary>
    public string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out var avatarHash) && avatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = avatarHash.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/{this.Id}/{avatarHash.GetString()}.{extension}";
            }
            else
                return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
        }
    }

    /// <summary>When the member joined the guild.</summary>
    public DateTimeOffset JoinDate => this.Json.GetProperty("joined_at").GetDateTimeOffset();

    /// <summary>Whether the member is "server" deafened in voice channels.</summary>
    public bool IsDeafened => this.Json.GetProperty("deaf").GetBoolean();

    /// <summary>Whether the member is "server" muted in voice channels.</summary>
    public bool IsMuted => this.Json.GetProperty("mute").GetBoolean();

    /// <summary>
    /// Whether the member has not yet met the guild's 
    /// <see href="https://support.discord.com/hc/en-us/articles/1500000466882">membership screening</see> requirements.
    /// </summary>
    public bool IsRestricted => this.Json.TryGetProperty("pending", out var property) && property.GetBoolean();

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public ValueTask<DiscordUser> GetUserAsync()
        => this.Bot.GetUserAsync(_userId);

    /// <summary></summary>
    public async ValueTask<EntityCollection<DiscordRole>> GetRolesAsync()
    {
        var guild = await GetGuildAsync();
        var roleIds = this.Json.GetProperty("roles");
        var roles = new Dictionary<ulong, DiscordRole>(roleIds.GetArrayLength());

        foreach (var roleId in roleIds.EnumerateArray())
        {
            var role = await guild.GetRoleAsync(roleId.GetUInt64());
            roles.Add(role.Id, role);
        }

        return new EntityCollection<DiscordRole>(roles);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the user is <see href="https://support.discord.com/hc/en-us/articles/360028038352">boosting</see> 
    /// its associated guild; <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="startDate">Date when the member started boosting its guild.</param>
    public bool IsBooster(out DateTimeOffset startDate)
    {
        if (this.Json.TryGetProperty("premium_since", out var property))
        {
            startDate = property.GetDateTimeOffset();
            return true;
        }
        else
        {
            startDate = DateTimeOffset.MinValue;
            return false;
        }
    }

    /// <summary></summary>
    public bool InTimeout(out DateTimeOffset expirationDate)
    {
        if (this.Json.TryGetProperty("communication_disabled_until", out var property) && property.ValueKind is not JsonValueKind.Null)
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

