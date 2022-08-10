namespace Donatello.Entity;

using Donatello.Enumeration;

/// <summary></summary>
public interface IChannel : IEntity
{
    /// <summary>Type of this channel.</summary>
    internal ChannelType Type { get; }

    /// <summary>Name of the channel.</summary>
    public string Name { get; }
}

