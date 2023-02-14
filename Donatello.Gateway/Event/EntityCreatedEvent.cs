namespace Donatello.Gateway.Event;

using Common.Entity;

/// <summary>Dispatched when an entity has been created or has otherwise become available.</summary>
public sealed class EntityCreatedEvent<TEntity> : ShardEvent where TEntity : ISnowflakeEntity
{
    /// <summary>The entity which was added or created.</summary>
    public TEntity Entity { get; internal init; }
}