namespace Donatello.Gateway.Event;

using System.Collections.ObjectModel;

/// <summary>Dispatched when a shard has successfully established or resumed a connection with Discord.</summary>
/// <remarks>This event only indicates that the initial handshake to create a session was sucessful; the data for these guilds will be sent by Discord shortly after this event is fired.</remarks>
public sealed class ConnectedEvent : DiscordEvent
{
    /// <summary>Snowflake IDs for each guild this shard is responsible for.</summary>
    public ReadOnlyCollection<Snowflake> GuildIds { get; internal set; }
}

