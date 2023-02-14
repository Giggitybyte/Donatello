namespace Donatello.Common.Entity;

using System;

/// <summary>An object representing a Discord entity which has a snowflake ID assigned to it.</summary>
public interface ISnowflakeEntity : IJsonEntity, IEquatable<ISnowflakeEntity>
{
    /// <summary>Unique snowflake identifier.</summary>
    public Snowflake Id { get; }
}

