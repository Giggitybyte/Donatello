namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System;

/// <summary></summary>
public class ChannelPinsUpdatedEvent : DiscordEvent
{
    /// <summary></summary>
    public DiscordGuild Guild { get; internal init; }

    /// <summary></summary>
    public DiscordChannel Channel { get; internal init; }

    public DateTimeOffset LastPinTimestamp { get; internal init; }
}

