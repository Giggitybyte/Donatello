﻿namespace Donatello.Gateway.Event;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Entity;

public class GuildEmojisUpdatedEvent : GuildEvent
{
    /// <summary></summary>
    public ReadOnlyCollection<GuildEmoji> UpdatedEmojis { get; internal init; }
    
    internal List<GuildEmoji> OutdatedEmojis { get; init; }
}