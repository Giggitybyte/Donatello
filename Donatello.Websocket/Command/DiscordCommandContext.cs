using System;
using Qmmands;

namespace Donatello.Websocket.Command
{
    public sealed class DiscordCommandContext : CommandContext
    {
        public DiscordCommandContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }
    }
}
