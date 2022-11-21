namespace Donatello.Entity;

using System.Threading.Tasks;

/// <summary>A Discord entity associated with a guild.</summary>
public interface IGuildEntity : ISnowflakeEntity
{
    /// <summary>Fetches the guild associated with this entity.</summary>
    public ValueTask<DiscordGuild> GetGuildAsync();
}

