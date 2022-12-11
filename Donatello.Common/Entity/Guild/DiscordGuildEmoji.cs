namespace Donatello.Entity.Guild;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class DiscordGuildEmoji : DiscordGuildEntity
{
    public DiscordGuildEmoji(DiscordBot bot, JsonElement entityJson, DiscordSnowflake guildId) 
        : base(bot, entityJson, guildId)
    {

    }

    /// <summary>Whether this emoji can be used.</summary>
    /// <remarks>Can be <see langword="false"/> when a guild loses a tier of Nitro.</remarks>
    public bool Available => this.Json.TryGetProperty("available", out JsonElement prop) is false || prop.GetBoolean();

    /// <summary>Whether this emoji is an animated GIF or WebP image.</summary>
    public bool Animated => this.Json.TryGetProperty("animated", out JsonElement prop) && prop.GetBoolean();

    /// <summary>Fetches the user which created this emoji.</summary>
    public async ValueTask<DiscordUser> GetCreatorAsync()
    {
        var guild = await this.GetGuildAsync();
        var user = new DiscordUser(this.Bot, this.Json.GetProperty("user"));

        this.Bot.UserCache.Add(user.Id, user);

        if (guild.MemberCache.Contains(user.Id, out JsonElement memberJson))
            return new DiscordGuildMember(this.Bot, this.GuildId, user, memberJson);
        else
            return user;
    }

    /// <summary>Fetches the roles allowed to use this emoji.</summary>
    public async IAsyncEnumerable<DiscordGuildRole> GetRolesAsync()
    {
        var roleIds = Array.Empty<DiscordSnowflake>();

        if (this.Json.TryGetProperty("roles", out JsonElement array))
            foreach (var roleId in array.EnumerateArray())
                roleIds[^1] = roleId.ToSnowflake();

        if (roleIds.Length is 0)
            yield break;

        var guild = await this.GetGuildAsync();
        foreach (var id in roleIds)
            yield return await guild.GetRoleAsync(id);
    }
}
