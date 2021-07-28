using System;
using System.Text.Json;
using System.Threading.Tasks;
using Qommon.Collections;

namespace Donatello.Websocket
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ExtensionBase
    {
        /// <summary>
        /// An active <see cref="DiscordBot"/> instance.
        /// </summary>
        protected DiscordBot Bot { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        protected ReadOnlyCollection<DiscordShard> Shards { get => new(Bot._shards); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        internal ExtensionBase(DiscordBot bot)
        {
            this.Bot = bot;
        }

        /// <summary>
        /// 
        /// </summary>
        protected ValueTask SendPayload(DiscordShard shard, int opcode, Action<Utf8JsonWriter> objectBuilder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        protected ValueTask SendPayload(DiscordShard client, GatewayPayload payload)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <b>Override this method.</b><para/>
        /// <i>Called by <see cref="DiscordBot.RemoveExtensionAsync{TExtension}"/> to safely stop the operation of the extension and release any resources in use.</i>
        /// </summary>
        protected virtual ValueTask CleanupAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
