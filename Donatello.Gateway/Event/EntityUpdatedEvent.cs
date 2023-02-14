namespace Donatello.Gateway.Event;

using Common.Entity;

/// <summary></summary>
public sealed class EntityUpdatedEvent<TEntity> : ShardEvent where TEntity : ISnowflakeEntity
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

