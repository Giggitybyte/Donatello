namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Entity.Channel;
using Extension;

/// <summary>A channel associated with a guild.</summary>
public abstract class GuildChannel : Channel, IGuildChannel
{
    private readonly Snowflake _guildId;

    public GuildChannel(JsonElement entityJson)
        : base(entityJson)
    {
        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            _guildId = prop.ToSnowflake();
        else
            throw new ArgumentException("JSON does not contain a guild ID.", nameof(entityJson));
    }

    public GuildChannel(JsonElement entityJson, Snowflake guildId)
        : base(entityJson)
    {
        _guildId = guildId;
    }


    /// <inheritdoc cref="IGuildChannel.Position"/>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary></summary>
    public bool Nsfw => throw new NotImplementedException();

    public Snowflake GuildId => _guildId;

    /// <inheritdoc cref="IGuildEntity.GetGuildAsync()"/>
    public ValueTask<Guild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public bool HasParent(out IGuildChannel parent)
        => throw new NotImplementedException();
}