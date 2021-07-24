using System;
using Qmmands;

namespace Donatello.Websocket.Commands
{
    public sealed class DiscordCommandContext : CommandContext
    {
        public DiscordCommandContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
