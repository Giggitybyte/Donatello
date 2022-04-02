namespace Donatello;

using Donatello.Entity;
using System.Collections;
using System.Collections.Generic;

/// <summary>Read-only collection of Discord entities accessable by ID.</summary>
public sealed class DiscordEntityCollection<TEntity> : IEnumerable<TEntity> where TEntity : DiscordEntity
{
    private IDictionary<ulong, TEntity> _entities;

    internal DiscordEntityCollection(IDictionary<ulong, TEntity> entities)
    {
        _entities = entities;
    }

    /// <summary></summary>
    public TEntity this[ulong entityId]
    {
        get
        {
            if (_entities.TryGetValue(entityId, out var entity))
                return entity;
            else
                return null;
        }
    }

    public IEnumerator<TEntity> GetEnumerator()
        => _entities.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => this.GetEnumerator();
}

