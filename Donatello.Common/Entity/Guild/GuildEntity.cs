namespace Donatello.Common.Entity.Guild;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Extension;

/// <summary>Discord entity associated with a guild.</summary>
public abstract class GuildEntity : Entity
{
    protected GuildEntity(JsonElement entityJson, Bot bot) : base(entityJson, bot)
    {
        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            this.GuildId = prop.ToSnowflake();
        else
            throw new ArgumentException("JSON does not contain a guild ID.", nameof(entityJson));
    }

    protected GuildEntity(JsonElement entityJson, Snowflake guildId, Bot bot) : base(entityJson, bot)
    {
        this.GuildId = guildId;
    }

    /// <summary></summary>
    public Snowflake GuildId { get; }

    /// <summary>Fetches the guild associated with this entity.</summary>
    public ValueTask<Guild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.GuildId);
}