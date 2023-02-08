namespace Donatello.Type;

using Entity;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="TEntity"/> instances.</summary> 
/// <typeparam name="TEntity">Stored entity type.</typeparam>
public sealed class EntityCache<TEntity> : ObjectCache<TEntity> where TEntity : class, ISnowflakeEntity
{
    /// <summary>Adds the provided <typeparamref name="TEntity"/> instance to this cache.</summary>
    internal TEntity Add(TEntity newEntity)
        => base.Add(newEntity.Id, newEntity);

    /// <summary>Adds a collection of entities to the cache.</summary>
    internal void AddMany(IEnumerable<TEntity> entities)
        => base.AddMany(entities, entity => entity.Id);

    /// <inheritdoc cref="ObjectCache{T}.TryGet(Snowflake, out T)"/>
    public new bool TryGet(Snowflake snowflake, out TEntity cachedEntity)
        => base.TryGet(snowflake, out cachedEntity);
}

