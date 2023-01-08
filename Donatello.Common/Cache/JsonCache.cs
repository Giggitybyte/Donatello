namespace Donatello.Cache;

using System;
using System.Collections.Generic;
using System.Text.Json;

public class JsonCache : ObjectCache<JsonElement>
{
    private Func<JsonElement, DiscordSnowflake> _idDelegate;

    public JsonCache(Func<JsonElement, DiscordSnowflake> idDelegate)
    {
        _idDelegate = idDelegate;
    }

    /// <summary>Adds the provided <see cref="JsonElement"/> instance to this cache.</summary>
    internal void Add(JsonElement newEntity)
        => base.Add(_idDelegate(newEntity), newEntity);

    /// <summary></summary>
    internal void AddMany(IEnumerable<JsonElement> entities)
        => base.AddMany(entities, _idDelegate);

    /// <summary>Adds the provided <paramref name="updatedEntity"/> to the cache and returns the previously cached object.</summary>
    internal JsonElement Replace(JsonElement updatedEntity)
        => base.Replace(_idDelegate(updatedEntity), updatedEntity);
}
