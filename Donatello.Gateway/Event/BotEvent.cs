namespace Donatello.Gateway.Event;

/// <summary></summary>
public class BotEvent : IEvent
{
    /// <summary>Bot instance which dispatched this event.</summary>
    public GatewayBot Bot { get; internal set; }
}
