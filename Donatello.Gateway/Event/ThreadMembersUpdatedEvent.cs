namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.ObjectModel;

/// <summary></summary>
public class ThreadMembersUpdatedEvent : DiscordEvent
{
    /// <summary></summary>
    public GuildThreadChannel Thread { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<ThreadMember> New { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<GuildMember> Old { get; internal set; }
}

