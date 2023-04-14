namespace Donatello.Common.Cache;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using Extension;

/// <summary>Dictionary-based cache for short-term storage of <see cref="JsonElement"/> instances.</summary> 
public class MemoryCache : IEnumerable<JsonElement>
{
    private sealed class Entry
    {
        public JsonElement Json { get; init; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessed { get; set; }
        public IDisposable TimerSubscription { get; init; }
    }
    
    private IObservable<long> _observableTimer;
    private ConcurrentDictionary<Snowflake, Entry> _cache;
    
    public MemoryCache()
    {
        this.InactiveLifetime = TimeSpan.FromMinutes(1);
        this.MaximumLifetime = TimeSpan.FromMinutes(5);
        
        _cache = new ConcurrentDictionary<Snowflake, Entry>();
        _observableTimer = Observable.Interval(TimeSpan.FromSeconds(5), Scheduler.Default);
    }
    
    public JsonElement this[Snowflake snowflake]
    {
        get
        {
            this.TryGetEntry(snowflake, out JsonElement cachedEntity);
            return cachedEntity;
        }

        set => this.AddOrUpdate(snowflake, value);
    }

    /// <summary>How long a cache entry can be inactive (e.g. not accessed) before it will be removed.</summary>
    internal TimeSpan InactiveLifetime { get; init; }

    /// <summary>The maximum amount of time an entry can be in the cache before it will be removed.</summary>
    internal TimeSpan MaximumLifetime { get; init; }
    
    /// <summary>Number of entries currently cached.</summary>
    public int Count => _cache.Count;

    /// <summary>Whether the provided snowflake has an entry in this cache. </summary>
    public bool ContainsEntry(Snowflake snowflake)
        => _cache.ContainsKey(snowflake);

    /// <summary>Returns <see langword="true"/> if the provided <paramref name="snowflake"/> has an entry associated with it, <see langword="false"/> otherwise.</summary>
    /// <param name="snowflake">Snowflake ID of the cache entry to retrieve</param>
    /// <param name="cachedObject">If the method returns <see langword="true"/> this parameter will contain the cached instance; otherwise it will be <see langword="null"/>.</param>
    public bool TryGetEntry(Snowflake snowflake, out JsonElement cachedObject)
    {
        if (_cache.TryGetValue(snowflake, out Entry entry))
        {
            entry.LastAccessed = DateTime.Now;
            cachedObject = entry.Json;
        }
        else
            cachedObject = default;
        
        return cachedObject.ValueKind is not JsonValueKind.Undefined;
    }

    /// <summary>
    /// Adds the provided <see cref="JsonElement"/> instance to this cache.
    /// If an instance with the same <paramref name="snowflake"/> is already present, then it will be replaced by the new <paramref name="instance"/>.
    /// </summary>
    /// <returns>Previously cached <see cref="JsonElement"/> instance if one was present, otherwise <see langword="null"/>.</returns>
    internal JsonElement AddOrUpdate(Snowflake snowflake, JsonElement instance)
    {
        var addDate = DateTime.Now;
        var newEntry = new Entry
        {
            Json = instance,
            LastAccessed = addDate,
            ExpiryDate = addDate + this.MaximumLifetime,
            TimerSubscription = _observableTimer.Subscribe(TimerElapsed)
        };

        _cache.TryRemove(snowflake, out Entry oldEntry);
        _cache[snowflake] = newEntry;

        return oldEntry?.Json ?? default;

        void TimerElapsed(long elapseCount)
        {
            if (_cache.TryGetValue(snowflake, out Entry cachedEntry))
            {
                var currentDate = DateTime.Now;
                if (currentDate >= cachedEntry.ExpiryDate || currentDate - cachedEntry.LastAccessed >= this.InactiveLifetime)
                {
                    cachedEntry.TimerSubscription.Dispose();
                    _cache.TryRemove(snowflake, out cachedEntry);
                }
            }
            else
                throw new InvalidOperationException("Timer subscription still active for removed entry.");
        }
    }

    /// <summary></summary>
    internal JsonElement AddOrUpdate(JsonElement instance)
        => this.AddOrUpdate(instance.GetProperty("id").ToSnowflake(), instance);

    /// <summary>Adds a collection of instances to the cache.</summary>
    internal void AddMany(IEnumerable<JsonElement> instances, Func<JsonElement, Snowflake> snowflakeDelegate)
    {
        foreach (var instance in instances) 
            this.AddOrUpdate(snowflakeDelegate(instance), instance);
    }

    /// <summary>Attempts to remove the entry associated with the provided <paramref name="snowflake"/> from the cache.</summary>
    internal JsonElement RemoveEntry(Snowflake snowflake)
    {
        if (_cache.TryRemove(snowflake, out Entry removedEntry))
        {
            removedEntry.TimerSubscription.Dispose();
            return removedEntry.Json;
        }
        else
            return default;
    }

    /// <summary>Removes all entries from this cache.</summary>
    internal void Clear()
    {
        foreach (var entry in _cache.Values)
            entry.TimerSubscription.Dispose();
        
        _cache.Clear();
    }

    IEnumerator<JsonElement> IEnumerable<JsonElement>.GetEnumerator()
    {
        foreach (var entry in _cache.Values)
        {
            entry.LastAccessed = DateTime.Now;
            yield return entry.Json;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => (this as IEnumerable<JsonElement>).GetEnumerator();
}