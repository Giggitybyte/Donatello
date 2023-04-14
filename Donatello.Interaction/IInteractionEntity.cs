namespace Donatello.Interaction;

using Common;
using Common.Entity;

/// <summary></summary>
public interface IInteractionEntity : ISnowflakeEntity
{
    /// <summary></summary>
    public Snowflake ApplicationId { get; }

    /// <summary></summary>
    public int Type { get; }

    /// <summary></summary>
    public string Token { get; }

    /// <summary></summary>
    public int Version { get; }
}