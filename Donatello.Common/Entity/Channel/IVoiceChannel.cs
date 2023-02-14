namespace Donatello.Common.Entity.Channel;

/// <summary>A channel which users are able to connect to and transmit audio to other connected users.</summary>
public interface IVoiceChannel : IChannel
{
    /// <summary>Audio bitrate in bits.</summary>
    public int Bitrate { get; }

    /// <summary>The maximum number of users allowed to join the channel.</summary>
    public int UserLimit { get; }
}

