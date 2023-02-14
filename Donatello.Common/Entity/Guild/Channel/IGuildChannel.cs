namespace Donatello.Common.Entity.Guild.Channel;

using Common.Entity.Channel;

public interface IGuildChannel : IGuildEntity, IChannel
{
    /// <summary>Position in the channel list.</summary>
    public int Position { get; }

    /// <summary>Whether this channel contains content which is age-restricted or otherwise unsuitable for viewing in public.</summary>
    public bool Nsfw { get; }

    /// <summary>Whether this channel is contained within another channel.</summary>
    public bool HasParent(out IGuildChannel parent);
}

