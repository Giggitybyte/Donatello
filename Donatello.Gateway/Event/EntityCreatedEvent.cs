namespace Donatello.Gateway.Event;

using Donatello.Entity;

public sealed class EntityCreatedEvent<TEntity> : DiscordEvent where TEntity : Entity
{
    /// <summary>The entity which was added or created.</summary>
    public TEntity Entity { get; internal set; }
}

