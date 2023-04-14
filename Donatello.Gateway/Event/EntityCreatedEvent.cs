namespace Donatello.Gateway.Event;

using Common.Entity;

/// <summary>Dispatched when an entity has been created or has otherwise become available.</summary>
public sealed class EntityCreatedEvent<TEntity> : EntityCreatedEvent where TEntity : ISnowflakeEntity
{
    public TEntity Entity { get; internal init; }
}

/// <summary>Dispatched when an entity has been created or has otherwise become available.</summary>
public class EntityCreatedEvent : ShardEvent
{
    internal static EntityCreatedEvent<TEntity> Create<TEntity>(TEntity instance) where TEntity : class, ISnowflakeEntity 
        => new() { Entity = instance };
}