namespace Donatello.Gateway.Event;

using Common.Entity;

/// <summary>Dispatched when the properties of an entity have been modified.</summary>
public sealed class EntityUpdatedEvent<TEntity> : EntityUpdatedEvent where TEntity : ISnowflakeEntity
{
    /// <summary>The updated entity received in this event.</summary>
    public TEntity UpdatedEntity { get; internal init; }

    /// <summary>The outdated entity which was previously cached.</summary>
    internal TEntity OutdatedEntity { get; init; }

    /// <summary>Returns <see langword="true"/> if an old instance of the entity was present in cache, <see langword="false"/> otherwise.</summary>
    /// <param name="outdatedEntity">If the method returns <see langword="true"/> this parameter will contain the outdated entity which was previously cached.
    /// Otherwise, it will be <see langword="null"/>.</param>
    public bool TryGetOutdatedEntity(out TEntity outdatedEntity)
    {
        outdatedEntity = this.OutdatedEntity;
        return outdatedEntity != null;
    }
}

/// <summary>Dispatched when the properties of an entity have been modified.</summary>
public class EntityUpdatedEvent : ShardEvent
{
    internal static EntityUpdatedEvent<TEntity> Create<TEntity>(TEntity updatedEntity, TEntity outdatedEntity) where TEntity : ISnowflakeEntity
        => new() { UpdatedEntity = updatedEntity, OutdatedEntity = outdatedEntity };
}