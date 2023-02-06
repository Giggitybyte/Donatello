namespace Donatello.Gateway.Event;

using Entity;
using System.Collections.ObjectModel;

/// <summary>All shards have connected successfully and all guilds are available.</summary>
public class ReadyEvent : BotEvent
{
    /// <summary>The bot user account associated with the newly created session.</summary>
    public User User { get; internal init; }

    /// <summary>All available guilds.</summary>
    public ReadOnlyCollection<Guild> Guilds { get; internal init; }
}