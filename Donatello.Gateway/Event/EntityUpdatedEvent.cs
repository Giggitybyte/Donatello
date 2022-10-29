namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary></summary>
public sealed class EntityUpdatedEvent<TEntity> : DiscordEvent where TEntity : DiscordEntity
{
    /// <summary>The updated entity received in this event.</summary>
    public TEntity UpdatedEntity { get; internal init; }

    /// <summary>The outdated entity which was previously cached.</summary>
    internal TEntity OutdatedEnity { get; init; }

    /// <summary>Returns <see langword="true"/> if an instance of the entity was present in cache, <see langword="false"/> otherwise.</summary>
    public bool TryGetOutdatedEntity(out TEntity outdatedEntity)
    {
        outdatedEntity = this.OutdatedEnity;
        return outdatedEntity != null;
    }

}

