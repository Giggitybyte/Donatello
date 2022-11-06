namespace Donatello.Gateway.Event;

using Donatello.Entity;

/// <summary>Dispatched when a shard has successfully established or resumed a connection with Discord.</summary>
/// <remarks>This event only indicates that the initial handshake to create a session was sucessful; not all guilds will have been made available at this point.</remarks>
public sealed class ConnectedEvent : DiscordEvent
{
}

