namespace Donatello.Gateway.Event;

using Donatello.Entity;

public sealed class EntityCreatedEvent<TEntity> : DiscordEvent where TEntity : DiscordEntity
{
    /// <summary>The entity which was created.</summary>
    public TEntity Entity { get; internal set; }
}

