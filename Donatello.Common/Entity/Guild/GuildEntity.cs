namespace Donatello.Entity;

using Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Discord entity associated with a guild.</summary>
public abstract class GuildEntity : Entity, IGuildEntity
{
    private readonly Snowflake _guildId;

    public GuildEntity(Bot bot, JsonElement entityJson, Snowflake guildId)
        : base(bot, entityJson)
    {
        _guildId = guildId;
    }

    public GuildEntity(Bot bot, JsonElement entityJson)
        : base(bot, entityJson)
    {
        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            _guildId = prop.ToSnowflake();
        else
            throw new ArgumentException("JSON does not contain a guild ID.", nameof(entityJson));
    }

    /// <summary></summary>
    internal protected Snowflake GuildId => _guildId;

    /// <inheritdoc cref="IGuildEntity.GetGuildAsync()"/>
    public ValueTask<Guild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    Snowflake IGuildEntity.GuildId => this.GuildId;
}