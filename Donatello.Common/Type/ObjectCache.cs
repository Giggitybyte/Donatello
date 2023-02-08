namespace Donatello.Type;

using Donatello;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="T"/> instances.</summary> 
/// <typeparam name="T">Stored object type.</typeparam>
public class ObjectCache<T> : IEnumerable<T>
{
    protected sealed class Entry
    {
        public T Object { get; init; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessed { get; set; }
        public IDisposable TimerSubscription { get; init; }
    }

    private TimeSpan _inactiveLifetime;
    private TimeSpan _maximumLifetime;
    private IObservable<long> _purgeTimer;
    private ConcurrentDictionary<Snowflake, Entry> _cache;

    /// <param name="inactiveLifetime">How long a cache entry can be inactive (e.g. not accessed) before it will be removed.</param>
    /// <param name="maximumLifetime">The maximum amount of time an entry can be in the cache before it will be removed.</param>
    internal ObjectCache(TimeSpan inactiveLifetime = default, TimeSpan maximumLifetime = default)
    {
        if (inactiveLifetime == default)
            _inactiveLifetime = TimeSpan.FromMinutes(15);

        if (maximumLifetime == default)
            _maximumLifetime = TimeSpan.FromHours(1);

        _cache = new ConcurrentDictionary<Snowflake, Entry>();
        _purgeTimer = Observable.Interval(TimeSpan.FromSeconds(5));
    }

    /// <summary>Number of entries currently cached.</summary>
    public int Count => _cache.Count;

    /// <summary>Gets or sets</summary>
    public T this[Snowflake snowflake]
    {
        get
        {
            this.TryGet(snowflake, out T cachedEntity);
            return cachedEntity;
        }

        set => this.Add(snowflake, value);
    }

    /// <summary>Whether the provided snowflake has an entry in this cache. </summary>
    public bool Contains(Snowflake snowflake)
        => _cache.ContainsKey(snowflake);

    /// <summary>Returns <see langword="true"/> if the provided <paramref name="snowflake"/> has an entry associated with it, <see langword="false"/> otherwise.</summary>
    /// <param name="snowflake"></param>
    /// <param name="cachedObject">If the method returns <see langword="true"/> this parameter will contain the cached instance; otherwise it will be <see langword="null"/>.</param>
    public bool TryGet(Snowflake snowflake, out T cachedObject)
    {
        if (_cache.TryGetValue(snowflake, out Entry entry))
        {
            entry.LastAccessed = DateTime.Now;
            cachedObject = entry.Object;
        }
        else
            cachedObject = default;

        return cachedObject != null;
    }

    /// <summary>Adds the provided <typeparamref name="T"/> instance to this cache.</summary>
    /// <remarks>If there is already an entry for the <paramref name="snowflake"/>, it will be replaced by <paramref name="newObject"/>.</remarks>
    /// <returns>The previously cached entry or, if one was not present, <see langword="null"/>.</returns>
    internal T Add(Snowflake snowflake, T newObject)
    {
        var addDate = DateTime.Now;
        var newEntry = new Entry()
        {
            Object = newObject,
            LastAccessed = addDate,
            ExpiryDate = addDate + _maximumLifetime,
            TimerSubscription = _purgeTimer.Subscribe(TimerElapsed)
        };

        _cache.TryRemove(snowflake, out Entry oldEntry);
        _cache[snowflake] = newEntry;

        void TimerElapsed(long elapseCount)
        {
            if (_cache.TryRemove(snowflake, out Entry cachedEntry))
            {
                var currentDate = DateTime.Now;
                if (currentDate >= cachedEntry.ExpiryDate || (currentDate - cachedEntry.LastAccessed >= _inactiveLifetime))
                    cachedEntry.TimerSubscription.Dispose();
            }
        }
    }

    /// <summary>Adds a collection of objects to the cache.</summary>
    /// <remarks>Snowflake keys are created for each entry using <paramref name="idDelegate"/>.</remarks>
    internal void AddMany(IEnumerable<T> newObjects, Func<T, Snowflake> idDelegate)
    {
        foreach (var newObject in newObjects)
        {
            var objectId = idDelegate(newObject);
            this.Add(objectId, newObject);
        }
    }

    /// <summary>Attempts to remove an object provided <paramref name="snowflake"/> to the cache and returns the previously cached object.</summary>
    internal bool TryRemove(Snowflake snowflake, out T removedObject)
    {
        removedObject = default;
        if (_cache.TryRemove(snowflake, out Entry removedEntry))
            removedObject = removedEntry.Object;

        return removedObject != null;
    }

    /// <summary>Removes all entries from this cache.</summary>
    internal void Clear()
        => _cache.Clear();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        foreach (var entry in _cache.Values)
        {
            entry.LastAccessed = DateTime.Now;
            yield return entry.Object;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => (this as IEnumerable<T>).GetEnumerator();
}