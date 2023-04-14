namespace Donatello.Common.Entity.Guild;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Extension;

/// <summary></summary>
public class GuildMember : GuildEntity
{
    public GuildMember(JsonElement json, Snowflake guildId, Bot bot) : base(json, guildId, bot)
    {
        if (json.TryGetProperty("user", out _) is false)
            throw new ArgumentException("JSON does not contain a user object.", nameof(json));
    }

    /// <summary></summary>
    protected internal JsonElement UserJson => base.Json.GetProperty("user");

    /// <summary></summary>
    public override Snowflake Id => this.UserJson.GetProperty("id").ToSnowflake();

    /// <summary>Global display name.</summary>
    public string Username => this.UserJson.GetProperty("username").GetString();

    /// <summary>Numeric sequence used to differentiate between members with the same username.</summary>
    public ushort Discriminator => ushort.Parse(this.UserJson.GetProperty("discriminator").GetString()!);

    /// <summary>When the user joined the guild.</summary>
    public DateTimeOffset JoinDate => this.Json.GetProperty("joined_at").GetDateTimeOffset();

    /// <summary>Whether the member is deafened in guild voice channels.</summary>
    public bool Deafened => this.Json.GetProperty("deaf").GetBoolean();

    /// <summary>Whether the member is muted in guild voice channels.</summary>
    public bool Muted => this.Json.GetProperty("mute").GetBoolean();

    /// <summary>Whether the member needs to pass the guild's <a href="https://support.discord.com/hc/en-us/articles/1500000466882">membership screening</a> requirements.</summary>
    /// <remarks>A pending member will not be able to interact with the guild until they pass the screening requirements.</remarks>
    public bool Pending => this.Json.TryGetProperty("pending", out JsonElement property) && property.GetBoolean();

    /// <summary>Member avatar URL.</summary>
    /// <remarks>If this member does not have a guild avatar, this property will return the user avatar instead.</remarks>
    public string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out JsonElement memberAvatarHash) && memberAvatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = memberAvatarHash.GetString()!.StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/guilds/{this.GuildId}/users/{this.Id}/avatars/{memberAvatarHash.GetString()}.{extension}";
            }
            else if (this.UserJson.TryGetProperty("avatar", out JsonElement userAvatarHash) && userAvatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = userAvatarHash.GetString()!.StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/{this.Id}/{userAvatarHash.GetString()}.{extension}";
            }
            else
            {
                var discriminator = this.UserJson.GetProperty("discriminator").GetString()!;
                return $"https://cdn.discordapp.com/embed/avatars/{ushort.Parse(discriminator) % 5}.png";
            }
        }
    }

    /// <summary>Returns <see langword="true"/> if the member has a nickname set, <see langword="false"/> otherwise.</summary>
    /// <param name="nickname">When the method returns <see langword="true"/> this parameter will contain the nickname set by the member; otherwise it'll contain an empty string.</param>
    public bool HasNickname(out string nickname)
    {
        nickname = this.Json.TryGetProperty("nick", out JsonElement prop) && prop.ValueKind is not JsonValueKind.Null
            ? prop.GetString()
            : string.Empty;

        return nickname != string.Empty;
    }

    /// <summary></summary>
    public async IAsyncEnumerable<Role> GetRolesAsync()
    {
        var guild = await this.GetGuildAsync();

        foreach (var roleId in this.Json.GetProperty("roles").EnumerateArray())
            yield return await guild.GetRoleAsync(roleId.GetUInt64());
    }

    /// <summary>Returns <see langword="true"/> if the member is <see href="https://support.discord.com/hc/en-us/articles/360028038352">boosting</see> its guild; <see langword="false"/> otherwise.</summary>
    /// <param name="startDate"> When the method returns <see langword="true"/> this parameter will contain the date when the member began boosting its guild; otherwise it'll be <see cref="DateTimeOffset.MinValue"/>.</param>
    public bool IsBooster(out DateTimeOffset startDate)
    {
        startDate = this.Json.TryGetProperty("premium_since", out JsonElement property)
            ? property.GetDateTimeOffset()
            : DateTimeOffset.MinValue;

        return startDate != DateTimeOffset.MinValue;
    }

    /// <summary>Returns <see langword="true"/> if a guild moderator has restricted this member from interacting with the guild.</summary>
    /// <param name="expirationDate">When the method returns <see langword="true"/> this parameter will contain date when the member will be allowed to interact with the guild; otherwise it'll be <see cref="DateTimeOffset.MinValue"/></param>
    public bool IsCommunicationDisabled(out DateTimeOffset expirationDate)
    {
        if (this.Json.TryGetProperty("communication_disabled_until", out JsonElement property) && property.ValueKind is not JsonValueKind.Null)
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