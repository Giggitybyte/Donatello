namespace Donatello.Gateway.Event;

using System.Collections.ObjectModel;
using Common;

/// <summary>Dispatched when a shard has successfully established or resumed a connection with Discord.</summary>
/// <remarks>This event only indicates that the initial handshake to create a session was successful;
/// guild data will be sent by Discord shortly after this event is fired.</remarks>
public sealed class ConnectedEvent : ShardEvent
{
    /// <summary>Snowflake IDs for each guild this shard is responsible for.</summary>
    public ReadOnlyCollection<Snowflake> GuildIds { get; internal set; }
}

