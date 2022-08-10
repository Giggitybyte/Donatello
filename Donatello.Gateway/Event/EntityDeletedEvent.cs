namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary></summary>
public sealed class EntityDeletedEvent<TEntity> : DiscordEvent where TEntity : DiscordEntity
{
    /// <summary></summary>
    public DiscordSnowflake EntityId { get; internal init; }

    /// <summary></summary>
    internal TEntity CachedEntity { get; init; }

    /// <summary>Returns <see langword="true"/> if an instance of the entity was present in cache, <see langword="false"/> otherwise.</summary>
    /// <param name="cachedEntity">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the last cached instance of the entity.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool TryGetCachedEntity(out TEntity cachedEntity)
    {
        cachedEntity = this.CachedEntity;
        return cachedEntity != null;
    }
}

