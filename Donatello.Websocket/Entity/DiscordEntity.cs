using System.Text.Json;

namespace Donatello.Websocket.Entity
{
    /// <summary>
    /// 
    /// </summary>
    internal abstract class DiscordEntity
    {
        /// <summary>
        /// JSON data backing this object.
        /// </summary>
        public JsonElement Json { get; protected set; }
    }
}
