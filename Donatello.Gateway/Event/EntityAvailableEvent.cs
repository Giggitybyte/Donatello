namespace Donatello.Gateway.Event;

using Donatello.Entity;

public sealed class EntityAvailableEvent<TEntity> : DiscordEvent where TEntity : DiscordEntity
{
    /// <summary>The entity which was added or created.</summary>
    public TEntity Entity { get; internal set; }
}

