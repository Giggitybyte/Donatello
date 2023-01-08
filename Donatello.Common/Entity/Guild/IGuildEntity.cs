﻿namespace Donatello.Entity;

using System.Threading.Tasks;

/// <summary>Discord entity associated with a guild.</summary>
public interface IGuildEntity : ISnowflakeEntity
{

    /// <summary></summary>
    public DiscordSnowflake GuildId { get; }

    /// <summary>Fetches the guild associated with this entity.</summary>
    public ValueTask<DiscordGuild> GetGuildAsync();
}

