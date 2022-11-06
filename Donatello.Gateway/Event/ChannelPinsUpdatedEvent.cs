namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>Dispatched when a message is pinned or unpinned in a text channel.</summary>
public class ChannelPinsUpdatedEvent : DiscordEvent
{
    /// <summary>Channel which had its pins updated.</summary>
    public DiscordGuildTextChannel Channel { get; internal set; }

    public IAsyncEnumerable<DiscordMessage> GetPinnedMessagesAsync()
        => this.Channel.GetPinnedMessagesAsync();
}

