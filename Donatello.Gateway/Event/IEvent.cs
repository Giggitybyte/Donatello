namespace Donatello.Gateway.Event;

using System;

public interface IEvent
{
    /// <summary>Bot instance which dispatched this event.</summary>
    public GatewayBot Bot { get; }
}
