namespace Donatello.Gateway.Event;

using Entity;
using System.Collections.ObjectModel;

/// <summary></summary>
public class ThreadMembersUpdatedEvent : GuildEvent
{
    /// <summary></summary>
    public GuildThreadChannel Thread { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<ThreadMember> New { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<GuildMember> Old { get; internal set; }
}

