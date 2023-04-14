namespace Donatello.Common.Cache;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>Collection of <see cref="MemoryCache"/> instances mapped to a parent snowflake ID.</summary> 
public sealed class AggregateCache : IReadOnlyDictionary<Snowflake, MemoryCache>
{
    private ConcurrentDictionary<Snowflake, MemoryCache> _innerCaches;

    internal AggregateCache()
    {
        _innerCaches = new ConcurrentDictionary<Snowflake, MemoryCache>();
    }

    /// <summary></summary>
    public MemoryCache this[Snowflake snowflake]
        => _innerCaches.GetOrAdd(snowflake, _ => new MemoryCache());

    /// <summary></summary>
    public IEnumerable<Snowflake> Keys => _innerCaches.Keys;

    /// <summary></summary>
    public IEnumerable<MemoryCache> Values => _innerCaches.Values;

    /// <summary></summary>
    public int Count => _innerCaches.Count;

    /// <summary>Whether the provided snowflake has a <see cref="MemoryCache"/> instance in this collection.</summary>
    public bool ContainsKey(Snowflake key) 
        => _innerCaches.ContainsKey(key);

    /// <summary>Attempts to get the <paramref name="innerCache"/> associated with the provided <paramref name="key"/>.</summary> 
    public bool TryGetValue(Snowflake key, out MemoryCache innerCache)
        => _innerCaches.TryGetValue(key, out innerCache);

    /// <summary>Removes all entries from each <see cref="MemoryCache"/> contained within this collection.</summary> 
    public void ClearAll()
    {
        foreach (var nestedCache in this.Values)
            nestedCache.Clear();
    }

    IEnumerator<KeyValuePair<Snowflake, MemoryCache>> IEnumerable<KeyValuePair<Snowflake, MemoryCache>>.GetEnumerator()
        => _innerCaches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => (this as IEnumerable<KeyValuePair<Snowflake, MemoryCache>>).GetEnumerator();
}