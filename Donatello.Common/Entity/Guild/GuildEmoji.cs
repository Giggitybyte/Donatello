namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class GuildEmoji : Entity, IGuildEntity
{
    private Snowflake _guildId;

    public GuildEmoji(Bot bot, JsonElement entityJson, Snowflake guildId)
        : base(bot, entityJson)
    {
        _guildId = guildId;
    }

    /// <summary></summary>
    public Snowflake GuildId => _guildId;

    /// <summary>Whether this emoji can be used.</summary>
    /// <remarks>Can be <see langword="false"/> when a guild loses a tier of Nitro.</remarks>
    public bool Available => this.Json.TryGetProperty("available", out JsonElement prop) is false || prop.GetBoolean();

    /// <summary>Whether this emoji is an animated GIF or WebP image.</summary>
    public bool Animated => this.Json.TryGetProperty("animated", out JsonElement prop) && prop.GetBoolean();

    /// <summary>Fetches the user which created this emoji.</summary>
    public async ValueTask<User> GetCreatorAsync()
    {
        var guild = await this.GetGuildAsync();
        var user = await this.Bot.GetUserAsync(this.Json.GetProperty("user_id").ToSnowflake());


        if (guild.MemberCache.TryGet(user.Id, out JsonElement memberJson))
            return new GuildMember(this.Bot, this.GuildId, user, memberJson);
        else
            return user;
    }

    public ValueTask<Guild> GetGuildAsync() => throw new NotImplementedException();

    /// <summary>Fetches the roles allowed to use this emoji.</summary>
    public async IAsyncEnumerable<Role> GetRolesAsync()
    {
        var roleIds = Array.Empty<Snowflake>();

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
