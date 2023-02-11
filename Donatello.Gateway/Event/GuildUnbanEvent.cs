namespace Donatello.Gateway.Event;

using Entity;

/// <summary>Dispatched when a user has been banned from a guild.</summary>
public class GuildUnbanEvent : GuildEvent
{
    /// <summary>User who was unbanned.</summary>
    public User User { get; internal init; }
}

