namespace Donatello.Gateway.Event;

/// <summary>Event received by a websocket shard.</summary>
public abstract class DiscordEvent : IEvent
{
    /// <summary>Bot instance which dispatched this event.</summary>
    public GatewayBot Bot { get; internal set; }

    /// <summary>The shard which received this event.</summary>
    public WebsocketShard Shard { get; internal set; }
}
