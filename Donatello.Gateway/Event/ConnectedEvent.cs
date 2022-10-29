namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary>Dispatched when a shard has successfully established a new session.</summary>
/// <remarks>This event only indicates that the initial handshake to create a session was sucessful; not all guilds will have been made available at this point.</remarks>
public sealed class ConnectedEvent : DiscordEvent
{
    /// <summary>The Discord user account associated with the new session.</summary>
    public DiscordUser User { get; internal set; }
}

