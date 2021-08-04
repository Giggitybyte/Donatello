using System;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Websocket.Bot;
using Qommon.Collections;

namespace Donatello.Websocket
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        internal BaseExtension(DiscordBot bot)
            => this.Bot = bot;

        /// <summary>
        /// An active <see cref="DiscordBot"/> instance.
        /// </summary>
        protected DiscordBot Bot { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        protected ReadOnlyCollection<DiscordShard> Shards { get => new(Bot.Shards.Values); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shard"></param>
        /// <param name="opcode"></param>
        /// <param name="objectBuilder"></param>
        /// <returns></returns>
        protected ValueTask SendPayloadAsync(DiscordShard shard, int opcode, Action<Utf8JsonWriter> objectBuilder)
            => shard.SendPayloadAsync(opcode, objectBuilder);

        /// <summary>
        /// Called by <see cref="DiscordBot"/> to safely stop the operation of the extension and release any resources in use.<para/>
        /// </summary>
        protected virtual ValueTask CleanupAsync()
            => ValueTask.CompletedTask;
    }
}
