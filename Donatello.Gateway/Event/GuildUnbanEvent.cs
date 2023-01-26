namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary>Dispatched when a user has been banned from a guild.</summary>
public class GuildUnbanEvent : DiscordEvent
{
    /// <summary>User who was unbanned.</summary>
    public User User { get; internal init; }

    /// <summary>Guild the user was unbanned from.</summary>
    public Guild Guild { get; internal init; }
}

