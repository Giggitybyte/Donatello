namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.ObjectModel;

/// <summary>All shards have connected successfully and all guilds are available.</summary>
public class ReadyEvent : DiscordEvent
{
    /// <summary>Each connected shard.</summary>
    public ReadOnlyCollection<DiscordWebsocketShard> Shards { get; internal init; }

    /// <summary>All available guilds.</summary>
    public ReadOnlyCollection<DiscordGuild> Guilds { get; internal init; }
}