namespace Donatello.Entity;

using System;
using System.Text.Json;

/// <summary>An object which has a snowflake ID assigned by Discord.</summary>
public interface IEntity : IEquatable<IEntity>
{
    /// <summary>Backing JSON object for this entity.</summary>
    protected internal JsonElement Json { get; }

    /// <summary>Bot instance which contains and manages this object.</summary>
    protected IBot Bot { get; }

    /// <summary>Unique snowflake identifier.</summary>
    public DiscordSnowflake Id { get; }
}

