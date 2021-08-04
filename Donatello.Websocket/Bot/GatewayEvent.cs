using System.Text.Json;

namespace Donatello.Websocket.Bot
{
    /// <summary>
    /// Transport object representing a gateway event received by a shard.
    /// </summary>
    internal readonly struct GatewayEvent
    {
        internal GatewayEvent(JsonElement payloadJson, DiscordShard sourceShard)
        {
            Payload = payloadJson;
            Shard = sourceShard;
        }

        /// <summary>
        /// 
        /// </summary>
        internal JsonElement Payload { get; }

        /// <summary>
        /// 
        /// </summary>
        internal DiscordShard Shard { get; }
    }
}
