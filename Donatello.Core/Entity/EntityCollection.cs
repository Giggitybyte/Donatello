namespace Donatello;

using Donatello.Entity;
using System.Collections;
using System.Collections.Generic;

/// <summary>Read-only collection of Discord entities accessable by snowflake ID.</summary>
public sealed class EntityCollection<TEntity> : IEnumerable<TEntity> where TEntity : DiscordEntity
{
    private static EntityCollection<TEntity> _emptyInstance;
    private IDictionary<ulong, TEntity> _entities;    

    static EntityCollection()
    {
        _emptyInstance = new EntityCollection<TEntity>(new Dictionary<ulong, TEntity>());
    }

    /// <summary></summary>
    public EntityCollection(IDictionary<ulong, TEntity> entities)
    {
        _entities = entities;
    }

    /// <summary></summary>
    public EntityCollection(IEnumerable<TEntity> entities)
    {
        _entities = new Dictionary<ulong, TEntity>();

        foreach (var entity in entities)
            _entities.Add(entity.Id, entity);
    }

    /// <summary></summary>
    public TEntity this[ulong entityId]
    {
        get
        {
            if (TryGetEntity(entityId, out var entity))
                return entity;
            else
                return null;
        }
    }

    /// <summary></summary>
    public static EntityCollection<TEntity> Empty => _emptyInstance;

    /// <summary></summary>
    public bool ContainsEntity(ulong entityId)
        => _entities.ContainsKey(entityId);

    /// <summary></summary>
    public bool TryGetEntity(ulong entityId, out TEntity entity)
        => _entities.TryGetValue(entityId, out entity);

    /// <inheritdoc/>
    public IEnumerator<TEntity> GetEnumerator()
        => _entities.Values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() 
        => this.GetEnumerator();
}

