namespace Donatello;

using Donatello.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="T"/> instances.</summary> 
/// <typeparam name="T">Stored object type.</typeparam>
public class ObjectCache<T>
{
    private class Entry
    {
        public T Object { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessed { get; set; }
        public IDisposable TimerSubscription { get; set; }
    }

    private ConcurrentDictionary<DiscordSnowflake, Entry> _cache;
    private TimeSpan _inactiveLifetime;
    private TimeSpan _maximumLifetime;
    private IObservable<long> _purgeTimer;

    /// <param name="inactiveLifetime">How long a cache entry can be inactive (e.g. not accessed) before it will be removed.</param>
    /// <param name="maximumLifetime">The maximum amount of time an entry can be in the cache before it will be removed.</param>
    internal protected ObjectCache(TimeSpan inactiveLifetime = default, TimeSpan maximumLifetime = default)
    {
        if (inactiveLifetime == default)
            _inactiveLifetime = TimeSpan.FromMinutes(15);

        if (maximumLifetime == default)
            _maximumLifetime = TimeSpan.FromHours(1);

        _cache = new ConcurrentDictionary<DiscordSnowflake, Entry>();
        _purgeTimer = Observable.Interval(TimeSpan.FromMinutes(5));
    }

    /// <summary>Amount of objects currently cached.</summary>
    public int Count => _cache.Count;

    /// <summary>Iterates over all <typeparamref name="T"/> instances contained within this cache.</summary>
    public IEnumerable<T> Enumerate()
    {
        foreach (var entry in _cache.Values)
        {
            entry.LastAccessed = DateTime.Now;
            yield return entry.Object;
        }
    }

    /// <summary>Returns <see langword="true"/> if the provided snowflake <paramref name="id"/> has an object associated with it, <see langword="false"/> otherwise.</summary>
    /// <param name="id"></param>
    /// <param name="cachedObject">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance,<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public virtual bool Contains(DiscordSnowflake id, out T cachedObject)
    {
        _cache.TryGetValue(id, out Entry entry);
        entry.LastAccessed = DateTime.Now;
        cachedObject = entry.Object;

        return cachedObject != null;
    }

    /// <summary>Adds the provided <typeparamref name="T"/> to this cache.</summary>
    internal protected virtual void Add(DiscordSnowflake id, T newObject)
    {
        var addDate = DateTime.Now;
        var newEntry = new Entry()
        {
            Object = newObject,
            LastAccessed = addDate,
            ExpiryDate = addDate + _maximumLifetime,
            TimerSubscription = _purgeTimer.Subscribe(i =>
            {
                if (_cache.TryGetValue(id, out Entry cachedEntry))
                {
                    var currentDate = DateTime.Now;
                    var inactiveTime = currentDate - cachedEntry.LastAccessed;

                    if (currentDate >= cachedEntry.ExpiryDate || inactiveTime >= _inactiveLifetime)
                        this.Remove(id);
                }
            })
        };

        _cache[id] = newEntry;
    }

    /// <summary>Adds the provided <paramref name="updatedObject"/> to the cache and returns the previously cached object.</summary>
    internal protected virtual T Replace(DiscordSnowflake id, T updatedObject)
    {
        var outdatedObject = this.Remove(id);
        this.Add(id, updatedObject);

        return outdatedObject;
    }

    /// <summary>Removes the entry assoicated with the provided <paramref name="id"/> and returns its <typeparamref name="T"/> value.</summary>
    internal protected virtual T Remove(DiscordSnowflake id)
    {
        if (_cache.TryRemove(id, out Entry entry))
        {
            entry.TimerSubscription.Dispose();
            return entry.Object;
        }
        else
            return default;
    }

    /// <summary>Removes all entries from this cache.</summary>
    internal protected void Clear()
        => _cache.Clear();
}

