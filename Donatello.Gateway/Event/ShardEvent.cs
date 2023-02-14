namespace Donatello.Gateway.Event;

/// <summary>Event received by a websocket shard.</summary>
public abstract class ShardEvent : BotEvent
{
    /// <summary>The shard which received this event.</summary>
    public WebsocketShard Shard { get; internal set; }
}
