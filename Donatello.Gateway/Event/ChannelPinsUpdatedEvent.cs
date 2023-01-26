namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.Generic;

/// <summary>Dispatched when a message is pinned or unpinned in a text channel.</summary>
public class ChannelPinsUpdatedEvent : DiscordEvent
{
    /// <summary>Channel which had its pins updated.</summary>
    public GuildTextChannel Channel { get; internal set; }

    public IAsyncEnumerable<Message> GetPinnedMessagesAsync()
        => this.Channel.GetPinnedMessagesAsync();
}

