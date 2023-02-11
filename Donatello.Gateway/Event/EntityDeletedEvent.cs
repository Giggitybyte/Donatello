namespace Donatello.Gateway.Event;

using Donatello;
using Entity;

/// <summary>Dispatched when an entity has been deleted or is otherwise inaccessible.</summary>
public sealed class EntityDeletedEvent<TEntity> : ShardEvent where TEntity : ISnowflakeEntity
{
    /// <summary></summary>
    public Snowflake EntityId { get; internal init; }

    /// <summary></summary>
    internal TEntity Instance { get; init; }

    /// <summary>Returns <see langword="true"/> if an instance of the entity was either sent by Discord or is present in the cache.</summary>
    /// <param name="cachedEntity"> If the method returns <see langword="true"/> this parameter will contain
    /// the last cached instance of the entity, otherwise it will be <see langword="null"/>.</param>
    public bool TryGetEntity(out TEntity cachedEntity)
    {
        cachedEntity = this.Instance;
        return cachedEntity != null;
    }
}

