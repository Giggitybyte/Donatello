namespace Donatello.Common.Entity.Guild;

/// <summary>Discord entity associated with a guild.</summary>
public interface IGuildEntity : ISnowflakeEntity
{
    /// <summary></summary>
    public Snowflake GuildId { get; }
}