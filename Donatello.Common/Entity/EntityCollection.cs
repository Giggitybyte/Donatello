namespace Donatello.Entity;

using System.Collections;
using System.Collections.Generic;

/// <summary>Read-only collection of Discord entities accessable by snowflake ID.</summary>
public sealed class EntityCollection<TEntity> : IEnumerable<TEntity> where TEntity : IEntity
{
    private readonly static EntityCollection<TEntity> _emptyInstance;
    private IDictionary<DiscordSnowflake, TEntity> _entities;    

    static EntityCollection()
    {
        var emptyDictionary = new Dictionary<DiscordSnowflake, TEntity>();
        _emptyInstance = new EntityCollection<TEntity>(emptyDictionary);
    }

    /// <summary></summary>
    public EntityCollection(IDictionary<DiscordSnowflake, TEntity> entities)
    {
        _entities = entities;
    }

    /// <summary></summary>
    public EntityCollection(IEnumerable<TEntity> entities)
    {
        _entities = new Dictionary<DiscordSnowflake, TEntity>();

        foreach (var entity in entities)
            _entities.Add(entity.Id, entity);
    }

    /// <summary></summary>
    public TEntity this[DiscordSnowflake entityId] => TryGetEntity(entityId, out var entity) ? entity : default;

    /// <summary></summary>
    public static EntityCollection<TEntity> Empty => _emptyInstance;


    /// <summary></summary>
    public bool ContainsEntity(DiscordSnowflake entityId)
        => _entities.ContainsKey(entityId);

    /// <summary></summary>
    public bool TryGetEntity(DiscordSnowflake entityId, out TEntity entity)
        => _entities.TryGetValue(entityId, out entity);

    /// <inheritdoc/>
    public IEnumerator<TEntity> GetEnumerator()
        => _entities.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}

