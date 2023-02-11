namespace Donatello.Gateway.Event;

using Entity;

/// <summary>Dispatched when a user has been banned from a guild.</summary>
public class GuildBanEvent : GuildEvent
{
    /// <summary>User who was banned.</summary>
    public User User { get; internal init; }
}

