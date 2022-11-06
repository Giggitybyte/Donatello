namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.ObjectModel;

/// <summary>All shards have connected successfully and all guilds are available.</summary>
public class ReadyEvent : DiscordEvent
{
    /// <summary>All available guilds.</summary>
    public ReadOnlyCollection<DiscordGuild> Guilds { get; internal init; }
}