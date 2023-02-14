namespace Donatello.Common.Entity.Channel;

using Enum;

/// <summary></summary>
public interface IChannel : ISnowflakeEntity
{
    /// <summary>Type of this channel.</summary>
    protected internal ChannelType Type { get; }

    /// <summary>Name of the channel.</summary>
    public string Name { get; }
}

