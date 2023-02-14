namespace Donatello.Common;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extension;

/// <summary>Memory-based cache for short-term storage of <see cref="JsonElement"/> instances.</summary> 
public sealed class JsonCache : IEnumerable<JsonElement>, IDisposable
{
    private sealed class Entry
    {
        public JsonElement Json { get; init; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    private ConcurrentDictionary<Snowflake, Entry> _cache;
    private Func<JsonElement, Snowflake> _snowflakeDelegate;
    private TimeSpan _inactiveLifetime;
    private TimeSpan _maximumLifetime;
    private Task _purgeTask;
    private CancellationTokenSource _disposalCts;

    /// <param name="snowflakeSelector">Function which takes a JSON object and returns its snowflake ID.</param>
    /// <param name="inactiveLifetime">How long a cache entry can be inactive (e.g. not accessed) before it will be removed.</param>
    /// <param name="maximumLifetime">The maximum amount of time an entry can be in the cache before it will be removed.</param>
    internal JsonCache(TimeSpan inactiveLifetime = default, TimeSpan maximumLifetime = default, Func<JsonElement, Snowflake> snowflakeSelector = null)
    {
        if (inactiveLifetime == default)
            _inactiveLifetime = TimeSpan.FromMinutes(5);

        if (maximumLifetime == default)
            _maximumLifetime = TimeSpan.FromMinutes(20);

        _snowflakeDelegate = snowflakeSelector ?? (json => json.GetProperty("id").ToSnowflake());
        _cache = new ConcurrentDictionary<Snowflake, Entry>();
        _disposalCts = new CancellationTokenSource();
        _purgeTask = PurgeStaleEntries(_disposalCts.Token);

        async Task PurgeStaleEntries(CancellationToken disposalToken)
        {
            while (!disposalToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), disposalToken).ContinueWith(delayTask =>
                {
                    var currentDate = DateTime.Now;
                    foreach (var (snowflake, entry) in _cache)
                        if (currentDate >= entry.ExpiryDate || (currentDate - entry.LastAccessed >= _inactiveLifetime))
                            _cache.Remove(snowflake, out _);
                });
        }
    }

    /// <summary>Number of entries currently cached.</summary>
    public int Count => _cache.Count;

    /// <summary>Gets or sets</summary>
    public JsonElement this[Snowflake snowflake]
    {
        get
        {
            this.TryGet(snowflake, out JsonElement cachedEntity);
            return cachedEntity;
        }

        set => this.Add(value);
    }

    /// <summary>Whether the provided snowflake has an entry in this cache. </summary>
    public bool Contains(Snowflake snowflake)
        => _cache.ContainsKey(snowflake);

    /// <summary>Returns <see langword="true"/> if the provided <paramref name="snowflake"/> has an entry associated with it, <see langword="false"/> otherwise.</summary>
    /// <param name="snowflake"></param>
    /// <param name="cachedObject">If the method returns <see langword="true"/> this parameter will contain the cached instance; otherwise it will be <see langword="null"/>.</param>
    public bool TryGet(Snowflake snowflake, out JsonElement cachedObject)
    {
        cachedObject = default;

        if (_cache.TryGetValue(snowflake, out Entry entry))
        {
            entry.LastAccessed = DateTime.Now;
            cachedObject = entry.Json;
        }

        return cachedObject.ValueKind != JsonValueKind.Undefined;
    }

    /// <summary>Adds the provided <see cref="JsonElement"/> instance to this cache.</summary>
    /// <remarks>If an entry with the same ID as <paramref name="json"/> is present it will be returned by this method.</remarks>
    internal JsonElement Add(JsonElement json)
    {
        var addDate = DateTime.Now;
        var newEntry = new Entry()
        {
            Json = json,
            LastAccessed = addDate,
            ExpiryDate = addDate + _maximumLifetime
        };

        var snowflake = _snowflakeDelegate(json);
        _cache.TryRemove(snowflake, out Entry oldEntry);
        _cache[snowflake] = newEntry;

        return oldEntry?.Json ?? default;
    }

    /// <summary>Adds a collection of objects to the cache.</summary>
    internal void AddMany(IEnumerable<JsonElement> newObjects)
    {
        foreach (var newObject in newObjects)
        {
            var objectId = _snowflakeDelegate(newObject);
            this.Add(newObject);
        }
    }

    /// <summary>Attempts to remove an object provided <paramref name="snowflake"/> to the cache and returns the previously cached object.</summary>
    internal bool TryRemove(Snowflake snowflake, out JsonElement removedObject)
    {
        removedObject = default;

        if (_cache.TryRemove(snowflake, out Entry removedEntry))
            removedObject = removedEntry.Json;

        return removedObject.ValueKind != JsonValueKind.Undefined;
    }

    /// <summary>Removes all entries from this cache.</summary>
    internal void Clear()
        => _cache.Clear();

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

    internal void Dispose()
    {
        if (_disposalCts.IsCancellationRequested is false)
        {
            _disposalCts.Cancel();

            _purgeTask.Wait();
            _cache.Clear();
        }
    }

    void IDisposable.Dispose() => this.Dispose();
}