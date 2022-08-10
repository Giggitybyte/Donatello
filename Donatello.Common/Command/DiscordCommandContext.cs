namespace Donatello.Command;

using Qmmands;
using System;

public abstract class DiscordCommandContext : CommandContext
{
    public DiscordCommandContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

