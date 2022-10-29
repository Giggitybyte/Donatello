namespace Donatello.Entity;

/// <summary>A channel which users are able to connect to and transmit audio to other connected users.</summary>
public interface IVoiceChannel : IChannel
{
    /// <summary>Bitrate in bits of the channel.</summary>
    public uint Bitrate { get; }

    /// <summary>The maximum number of users allowed to join the channel.</summary>
    public uint UserLimit { get; }

    public 
}

