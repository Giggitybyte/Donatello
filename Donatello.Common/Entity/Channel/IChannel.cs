namespace Donatello.Entity;

using Donatello.Enum;

/// <summary></summary>
public interface IChannel : IEntity
{
    /// <summary>Type of this channel.</summary>
    protected internal ChannelType Type { get; }

    /// <summary>Name of the channel.</summary>
    public string Name { get; }
}

