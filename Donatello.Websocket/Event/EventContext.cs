using System;
using System.Text.Json;
using Donatello.Websocket.Bot;

namespace Donatello.Websocket.Event
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class EventContext : EventArgs
    {
        /// <summary>
        /// JSON event payload backing this object.
        /// </summary>
        internal JsonElement Payload { get; set; }

        /// <summary>
        /// The shard connection which received the event.
        /// </summary>
        public DiscordShard Shard { get; internal set; }
    }
}
