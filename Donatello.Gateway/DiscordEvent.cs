namespace Donatello.Gateway;

using System.Text.Json;

/// <summary>Transport object representing a gateway event received by a shard.</summary>
internal readonly struct DiscordEvent
{
    internal DiscordEvent(DiscordShard shard, JsonElement payload)
    {
        Shard = shard;
        Payload = payload;
    }

    /// <summary>Shard which received the event payload.</summary>
    internal DiscordShard Shard { get; }

    /// <summary>Event payload JSON.</summary>
    internal JsonElement Payload { get; }
}
