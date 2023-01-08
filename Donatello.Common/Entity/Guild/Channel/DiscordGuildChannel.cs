namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel associated with a guild.</summary>
public abstract class DiscordGuildChannel : DiscordChannel, IGuildChannel
{
    private readonly DiscordSnowflake _guildId;

    public Channel(DiscordBot bot, JsonElement entityJson)
        : base(bot, entityJson)
    {
        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            _guildId = prop.ToSnowflake();
        else
            throw new ArgumentException("JSON does not contain a guild ID.", nameof(entityJson));
    }

    /// <inheritdoc cref="IGuildChannel.Position"/>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary></summary>
    public bool Nsfw => throw new NotImplementedException();

    public DiscordSnowflake GuildId => _guildId;

    /// <inheritdoc cref="IGuildEntity.GetGuildAsync()"/>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public bool HasParent(out IGuildChannel parent)
        => throw new NotImplementedException();
}