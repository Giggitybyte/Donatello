namespace Donatello;

using Donatello.Entity;
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

    public EntityCollection(IDictionary<DiscordSnowflake, TEntity> entities)
    {
        _entities = entities;
    }

    public EntityCollection(IEnumerable<TEntity> entities)
    {
        _entities = new Dictionary<DiscordSnowflake, TEntity>();

        foreach (var entity in entities)
            _entities.Add(entity.Id, entity);
    }

    /// <summary></summary>
    public TEntity this[DiscordSnowflake entityId] => this.TryGetEntity(entityId, out var entity) ? entity : default;

    /// <summary>Read-only instance which contains zero entities.</summary>
    public static EntityCollection<TEntity> Empty => _emptyInstance;

    /// <summary>Returns <see langword="true"/> if this collection has an entity with the provided snowflake ID, <see langword="false"/> otherwise.</summary>
    public bool ContainsEntity(DiscordSnowflake id)
        => _entities.ContainsKey(id);

    /// <summary>Returns <see langword="true"/> if this collection has an entity with the provided snowflake ID, <see langword="false"/> otherwise.</summary>
    /// <param name="entity">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the previously cached instance,<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool TryGetEntity(DiscordSnowflake entityId, out TEntity entity)
        => _entities.TryGetValue(entityId, out entity);

    /// <inheritdoc/>
    public IEnumerator<TEntity> GetEnumerator()
        => _entities.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

