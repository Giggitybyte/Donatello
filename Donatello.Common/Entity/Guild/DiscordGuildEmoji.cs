namespace Donatello.Entity.Guild;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    /// <inheritdoc cref="IBotEntity.Bot"/>
    internal DiscordBot Bot { get; private init; }

    /// <summary></summary>
    public DiscordSnowflake Id => this.Json.GetProperty("id").ToSnowflake();

    /// <summary></summary>
    public bool Available => this.Json.TryGetProperty("available", out JsonElement prop) is false || prop.GetBoolean();

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync() 
        => this.Bot.

    /// <summary></summary>
    public bool HasRoles(out ReadOnlyCollection<DiscordSnowflake> roleIds)
    {
        

        if (_guildId is not null && this.Json.TryGetProperty("roles", out JsonElement roles))
            foreach (var roleId in roles.EnumerateArray())
                return roleId.ToSnowflake();
        else
            return false;
    }

    DiscordBot IBotEntity.Bot => this.Bot;

    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other)
        => this.Id.Equals(other.Id);
}
