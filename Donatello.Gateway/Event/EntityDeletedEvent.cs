namespace Donatello.Gateway.Event;

using Donatello;
using Donatello.Entity;

/// <summary>Fired when an entity has been deleted or is otherwise inaccessable.</summary>
public sealed class EntityDeletedEvent<TEntity> : DiscordEvent where TEntity : DiscordEntity
{
    /// <summary></summary>
    public DiscordSnowflake EntityId { get; internal init; }

    /// <summary></summary>
    internal TEntity Instance { get; init; }

    /// <summary>Returns <see langword="true"/> if an instance of the entity was present in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="cachedEntity">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the last cached instance of the entity.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool TryGetEntity(out TEntity cachedEntity)
    {
        cachedEntity = this.Instance;
        return cachedEntity != null;
    }
}

