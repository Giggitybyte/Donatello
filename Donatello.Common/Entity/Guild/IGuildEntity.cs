namespace Donatello.Entity;

using System.Threading.Tasks;

/// <summary>A Discord entity contained within a guild.</summary>
public interface IGuildEntity : IEntity
{
    /// <summary>Fetches the guild which contains this entity.</summary>
    public ValueTask<DiscordGuild> GetGuildAsync();
}

