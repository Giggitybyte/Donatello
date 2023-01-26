namespace Donatello.Type;

using Donatello;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;

/// <summary>Memory-based cache for short-term storage of <typeparamref name="T"/> instances.</summary> 
/// <typeparam name="T">Stored object type.</typeparam>
public class ObjectCache<T> : IEnumerable<T>
{
    protected sealed class Entry
    {
        public T Object { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessed { get; set; }
        public IDisposable TimerSubscription { get; set; }
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
        _purgeTimer = Observable.Interval(TimeSpan.FromSeconds(1));
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

    /// <summary>Returns <see langword="true"/> if the provided <paramref name="snowflake"/> has a cache entry associated with it, <see langword="false"/> otherwise.</summary>
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
            cachedObject = default(T);

        return !cachedObject.Equals(default(T));
    }

    /// <summary>Adds the provided <typeparamref name="T"/> instance to this cache.</summary>
    /// <remarks>If there is already an entry for the <paramref name="snowflake"/>, it will be replaced by <paramref name="newObject"/>.</remarks>
    internal void Add(Snowflake snowflake, T newObject)
    {
        var addDate = DateTime.Now;
        var newEntry = new Entry()
        {
            Object = newObject,
            LastAccessed = addDate,
            ExpiryDate = addDate + _maximumLifetime,
            TimerSubscription = _purgeTimer.Subscribe(TimerElasped)
        };

        _cache[snowflake] = newEntry;

        void TimerElasped(long elapseCount)
        {
            if (_cache.TryGetValue(snowflake, out Entry cachedEntry))
            {
                var currentDate = DateTime.Now;

                if (currentDate >= cachedEntry.ExpiryDate || currentDate - cachedEntry.LastAccessed >= _inactiveLifetime)
                    this.Remove(snowflake);
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

    /// <summary>Adds the provided <paramref name="updatedObject"/> to the cache and returns the previously cached object.</summary>
    internal T Replace(Snowflake snowflake, T updatedObject)
    {
        var outdatedObject = this.Remove(snowflake);
        this.Add(snowflake, updatedObject);

        return outdatedObject;
    }

    /// <summary>Removes the entry assoicated with the provided <paramref name="snowflake"/> and returns its <typeparamref name="T"/> value.</summary>
    /// <remarks>If the snowflake does not currently exist in the cache, this method will return <see langword="null"/>.</remarks>
    internal T Remove(Snowflake snowflake)
    {
        if (_cache.TryRemove(snowflake, out Entry entry))
        {
            entry.TimerSubscription.Dispose();
            return entry.Object;
        }
        else
            return default;
    }

    /// <summary>Removes the entry assoicated with the provided <paramref name="snowflakes"/> and returns each <typeparamref name="T"/> value.</summary>
    internal IEnumerable<T> RemoveMany(IEnumerable<Snowflake> snowflakes)
    {
        foreach (var id in snowflakes)
            yield return this.Remove(id);
    }

    /// <summary>Removes all entries from this cache.</summary>
    internal void Clear()
        => _cache.Clear();

    /// <summary>Returns an enumerator which iterates through the cache.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var entry in _cache.Values)
        {
            entry.LastAccessed = DateTime.Now;
            yield return entry.Object;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

