using System.Text.Json;

namespace Donatello.Websocket.Bot
{
    /// <summary>
    /// Transport object representing a gateway event received by a shard.
    /// </summary>
    internal readonly struct GatewayEvent
    {
        internal GatewayEvent(int shardId, JsonElement payload)
        {
            Payload = payload;
            ShardId = shardId;
        }

        /// <summary>
        /// ID of the shard which received the event payload.
        /// </summary>
        internal int ShardId { get; }

        /// <summary>
        /// JSON event payload.
        /// </summary>
        internal JsonElement Payload { get; }
    }
}
