namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Discord entity associated with a guild.</summary>
public abstract class DiscordGuildEntity : DiscordEntity, IGuildEntity
{
    private readonly DiscordSnowflake _guildId;

    public DiscordGuildEntity(DiscordBot bot, JsonElement entityJson, DiscordSnowflake guildId)
        : base(bot, entityJson)
    {
        _guildId = guildId;
    }

    public DiscordGuildEntity(DiscordBot bot, JsonElement entityJson)
        : base(bot, entityJson)
    {
        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            _guildId = prop.ToSnowflake();
        else
            throw new ArgumentException("JSON does not contain a guild ID.", nameof(entityJson));
    }

    /// <summary></summary>
    internal protected DiscordSnowflake GuildId => _guildId;

    /// <inheritdoc cref="IGuildEntity.GetGuildAsync()"/>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);
}