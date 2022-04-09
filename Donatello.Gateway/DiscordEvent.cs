namespace Donatello.Gateway;

using System.Text.Json;

/// <summary>Transport object representing a gateway event received by a shard.</summary>
internal readonly struct DiscordEvent
{
    internal DiscordEvent(DiscordWebsocketShard shard, JsonElement payload)
    {
        this.Shard = shard;
        this.Payload = payload;
    }

    /// <summary>Shard which received the event payload.</summary>
    internal DiscordWebsocketShard Shard { get; }

    /// <summary>JSON event payload.</summary>
    internal JsonElement Payload { get; }
}
