namespace Donatello.Gateway.Event;

using Donatello.Entity;
using System.Collections.ObjectModel;

/// <summary></summary>
public class ThreadMembersUpdatedEvent : DiscordEvent
{
    /// <summary></summary>
    public DiscordThreadTextChannel Thread { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<DiscordThreadMember> NewMembers { get; internal set; }

    /// <summary></summary>
    public ReadOnlyCollection<DiscordGuildMember> OldMembers { get; internal set; }
}

