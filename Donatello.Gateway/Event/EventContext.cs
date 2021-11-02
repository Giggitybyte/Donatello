namespace Donatello.Gateway.Event;

using System;
using System.Text.Json;

/// <summary>
/// 
/// </summary>
public abstract class EventContext : EventArgs
{
    /// <summary>
    /// JSON event payload backing this object.
    /// </summary>
    internal JsonElement Payload { get; init; }

    /// <summary>
    /// The shard connection which received the event.
    /// </summary>
    public DiscordShard Shard { get; internal init; }
}
