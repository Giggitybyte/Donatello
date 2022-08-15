namespace Donatello.Entity;

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="TEntity"/> instances.</summary>
public class EntityCache<TEntity> where TEntity : IEntity
{
    private MemoryCache _cache;
    private MemoryCacheEntryOptions _entryConfig;

    internal EntityCache(TimeSpan slidingExpiration = default, TimeSpan absoluteExpiration = default)
    {
        if (slidingExpiration == default)
            slidingExpiration = TimeSpan.FromMinutes(30);

        if (absoluteExpiration == default)
            absoluteExpiration = TimeSpan.FromHours(1);

        _cache = new MemoryCache(new MemoryCacheOptions());
        _entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(slidingExpiration)
            .SetAbsoluteExpiration(absoluteExpiration);
    }

    /// <summary>Amount of entities currently cached.</summary>
    public int Count => _cache.Count;

    /// <summary>Adds the provided <typeparamref name="TEntity"/> to this cache.</summary>
    internal void Add(TEntity entity)
        => _cache.Set(entity.Id, entity, _entryConfig);

    /// <summary>Adds the provided <paramref name="entity"/> to the cache and returns the previously cached value.</summary>
    internal TEntity GetAndUpdate(TEntity entity)
    {
        _cache.TryGetValue(entity.Id, out TEntity outdatedEntity);
        _cache.Set(entity.Id, entity, _entryConfig);

        return outdatedEntity;
    }

    /// <summary>Returns <see langword="true"/> if the provided snowflake has an existing entry in this cache, <see langword="false"/> otherwise.</summary>
    /// <param name="entity">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance,<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool TryGetEntity(DiscordSnowflake id, out TEntity entity) 
        => _cache.TryGetValue(id, out entity);

    /// <summary>Returns <see langword="true"/> if the provided snowflake had an existing entry in this cache, <see langword="false"/> otherwise.</summary>
    /// <param name="entity">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the previously cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    internal bool TryRemoveEntity(DiscordSnowflake id, out TEntity entity)
    {
        if (_cache.TryGetValue(id, out entity))
            _cache.Remove(id);

        return entity != null;
    }

    /// <summary>Removes all entities from this cache and replaces them with <paramref name="newValues"/>.</summary>
    internal void ReplaceAll(IEnumerable<TEntity> newValues)
    {
        _cache.Dispose();
        _cache = new MemoryCache(new MemoryCacheOptions());

        foreach (TEntity entity in newValues)
            _cache.Set(entity.Id, entity, _entryConfig);
    }
}

