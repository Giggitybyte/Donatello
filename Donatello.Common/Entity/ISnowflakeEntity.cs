namespace Donatello.Entity;

using System;
using Donatello;

/// <summary>An object representing a Discord entity which has a snowflake ID assigned to it.</summary>
public interface ISnowflakeEntity : IJsonEntity, IEquatable<ISnowflakeEntity>
{
    /// <summary>Unique snowflake identifier.</summary>
    public DiscordSnowflake Id { get; }
}

