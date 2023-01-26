namespace Donatello.Gateway.Event;

internal interface IEvent
{
    /// <summary>Bot instance which dispatched this event.</summary>
    public GatewayBot Bot { get; }
}
