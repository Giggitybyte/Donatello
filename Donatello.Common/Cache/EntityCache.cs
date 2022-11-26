namespace Donatello.Cache;

using Donatello;
using Donatello.Entity;
using System;
using System.Collections.Generic;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="TEntity"/> instances.</summary> 
/// <typeparam name="TEntity">Stored entity type.</typeparam>
public sealed class EntityCache<TEntity> : ObjectCache<TEntity> where TEntity : class, ISnowflakeEntity
{
    /// <summary>Adds the provided <typeparamref name="TEntity"/> instance to this cache.</summary>
    internal void Add(TEntity newEntity)
        => base.Add(newEntity.Id, newEntity);

    /// <summary></summary>
    internal void AddMany(IEnumerable<TEntity> entities)
        => base.AddMany(entities, entity => entity.Id);


    internal bool Contains<T>(DiscordSnowflake snowflake, out T cachedEntity) where T : ISnowflakeEntity
        => base.Contains(snowflake, out cachedEntity);

    /// <summary>Adds the provided <paramref name="updatedEntity"/> to the cache and returns the previously cached object.</summary>
    internal TEntity Replace(TEntity updatedEntity)
        => base.Replace(updatedEntity.Id, updatedEntity);
}

