using System;
using Qmmands;

namespace Donatello.Websocket.Command
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DiscordCommandContext : CommandContext
    {
        public DiscordCommandContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }
    }
}
