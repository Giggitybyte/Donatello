namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary>Dispatched when a user has been banned from a guild.</summary>
public class GuildBanEvent : DiscordEvent
{
    /// <summary>User who was banned.</summary>
    public DiscordUser User { get; internal init; }

    /// <summary>Guild the user was banned from.</summary>
    public DiscordGuild Guild { get; internal init; }
}

