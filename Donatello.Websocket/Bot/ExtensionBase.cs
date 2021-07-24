using System;
using System.Threading.Tasks;
using Donatello.Websocket.Client;
using Donatello.Websocket.Payload;
using Qommon.Collections;

namespace Donatello.Websocket.Bot
{
    public abstract class ExtensionBase
    {
        /// <summary>
        /// An active <see cref="DiscordBot"/> instance.
        /// </summary>
        protected DiscordBot Bot { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        protected ReadOnlyCollection<DiscordClient> Shards { get => new(Bot._shards); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        internal ExtensionBase(DiscordBot bot)
        {
            Bot = bot;
        }

        /// <summary>
        /// 
        /// </summary>
        protected Task SendPayload(GatewayPayload payload)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        protected Task SendPayload(DiscordClient client, GatewayPayload payload)
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
