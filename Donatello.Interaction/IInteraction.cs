namespace Donatello.Interaction;

using Entity;

/// <summary></summary>
public interface IInteraction : ISnowflakeEntity
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