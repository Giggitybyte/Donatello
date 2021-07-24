using System.Text.Json.Serialization;

namespace Donatello.Websocket.Payload.Incoming
{
    internal class GatewayInfoPayload
    {
        [JsonPropertyName("url")]
        public string WebsocketUrl { get; set; }

        [JsonPropertyName("shards")]
        public int ShardCount { get; set; }

        [JsonPropertyName("session_start_limit")]
        public SessionStartLimits SessionLimits { get; set; }

        public class SessionStartLimits
        {
            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("remaining")]
            public int Remaining { get; set; }

            [JsonPropertyName("reset_after")]
            public int ResetAfter { get; set; }

            [JsonPropertyName("max_concurrency")]
            public int MaxConcurrency { get; set; }
        }
    }


}
