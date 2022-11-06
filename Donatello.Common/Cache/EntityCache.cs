namespace Donatello.Entity;

using System.Collections.Generic;
using System.Linq;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="TEntity"/> instances.</summary> 
/// <typeparam name="TEntity">Stored object type.</typeparam>
public sealed class EntityCache<TEntity> : ObjectCache<TEntity> where TEntity : IEntity
{
    /// <summary>Adds the provided <typeparamref name="TEntity"/> instance to this cache.</summary>
    internal void Add(TEntity newEntity)
        => base.Add(newEntity.Id, newEntity);

    /// <summary></summary>
    internal void AddMany(IList<TEntity> entities)
        => base.AddMany(entities, entity => entity.Id);

    /// <summary>Adds the provided <paramref name="updatedEntity"/> to the cache and returns the previously cached object.</summary>
    internal TEntity Replace(TEntity updatedEntity)
        => base.Replace(updatedEntity.Id, updatedEntity);

    /// <summary>Removes the entry assoicated with the provided <paramref name="snowflake"/> and returns its <typeparamref name="TEntity"/> value.</summary>
    internal TEntity Remove(TEntity entity)
        => base.Remove(entity.Id);

    /// <summary>Removes the entry assoicated with the provided <paramref name="entities"/> and returns each <typeparamref name="TEntity"/> value.</summary>
    internal IEnumerable<TEntity> RemoveMany(IList<TEntity> entities) 
        => base.RemoveMany(entities.Select(entity => entity.Id));
}

