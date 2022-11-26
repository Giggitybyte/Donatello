namespace Donatello.Entity.Guild;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class DiscordGuildEmoji : DiscordEmoji, ISnowflakeEntity, IGuildEntity, IBotEntity
{
    private DiscordSnowflake _guildId;

    internal DiscordGuildEmoji(JsonElement json, DiscordSnowflake guildId)
        : base(json)
    {
        _guildId = guildId;
    }

    /// <summary>Whether this emoji can be used.</summary>
    /// <remarks>Can be <see langword="false"/> when a guild loses a tier of Nitro.</remarks>
    public bool Available => this.Json.TryGetProperty("available", out JsonElement prop) is false || prop.GetBoolean();

    /// <summary>Whether this emoji is an animated GIF or WebP image.</summary>
    public bool Animated => this.Json.TryGetProperty("animated", out JsonElement prop) && prop.GetBoolean();

    /// <inheritdoc cref="IBotEntity.Bot"/>
    internal DiscordBot Bot { get; private init; }

    /// <inheritdoc cref="ISnowflakeEntity.Id"/>
    public DiscordSnowflake Id => this.Json.GetProperty("id").ToSnowflake();

    /// <summary>Fetches the user which created this emoji.</summary>
    public ValueTask<DiscordGuildMember> GetCreatorAsync()
    {

    }

    /// <summary>Fetches the roles allowed to use this emoji.</summary>
    public async IAsyncEnumerable<DiscordGuildRole> GetRolesAsync()
    {
        var roleIds = Array.Empty<DiscordSnowflake>();

        if (_guildId is not null && this.Json.TryGetProperty("roles", out JsonElement array))
            foreach (var roleId in array.EnumerateArray())
                roleIds[^1] = roleId.ToSnowflake();

        if (roleIds.Length is 0)
            yield break;

        var guild = await this.GetGuildAsync();
        foreach (var id in roleIds)
            yield return await guild.GetRoleAsync(id);
    }

    /// <inheritdoc cref="IGuildEntity.GetGuildAsync"/>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    DiscordBot IBotEntity.Bot => this.Bot;
    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => this.Id.Equals(other.Id);
}
