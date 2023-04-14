namespace Donatello.Gateway.Event;

using Common.Entity.User;

/// <summary>Dispatched when a user has been banned from a guild.</summary>
public class UnbanEvent : GuildEvent
{
    /// <summary>User who was unbanned.</summary>
    public User User { get; internal init; }
}

