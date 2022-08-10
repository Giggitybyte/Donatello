namespace Donatello.Gateway.Event;

/// <summary>Event received by a websocket shard.</summary>
public class DiscordEvent
{
    /// <summary>The shard which received this event.</summary>
    public DiscordWebsocketShard Shard { get; internal init; }

    /// <summary>Bot instance which dispatched this event.</summary>
    public DiscordGatewayBot Bot { get; internal set; }
}
