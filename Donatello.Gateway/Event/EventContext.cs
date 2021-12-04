namespace Donatello.Gateway.Event;

using System;
using System.Text.Json;

public abstract class EventContext : EventArgs
{
    /// <summary>JSON event payload backing this object.</summary>
    internal JsonElement Payload { get; init; }
}
