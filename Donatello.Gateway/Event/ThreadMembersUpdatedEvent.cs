namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.ObjectModel;

/// <summary></summary>
public class ThreadMembersUpdatedEvent : DiscordEvent
{
    /// <summary></summary>
    public DiscordThreadChannel Thread { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<DiscordThreadMember> New { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<DiscordGuildMember> Old { get; internal set; }
}

