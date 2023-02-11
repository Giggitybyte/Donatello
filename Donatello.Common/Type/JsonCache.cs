namespace Donatello.Type;

using System;
using System.Collections.Generic;
using System.Text.Json;

public class JsonCache : ObjectCache<JsonElement>
{
    private Func<JsonElement, Snowflake> _idDelegate;

    public JsonCache(Func<JsonElement, Snowflake> idDelegate)
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
