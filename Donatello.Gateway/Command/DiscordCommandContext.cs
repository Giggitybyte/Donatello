namespace Donatello.Gateway.Command;

using System;
using Qmmands;

/// <summary>
/// 
/// </summary>
public sealed class DiscordCommandContext : CommandContext
{
    public DiscordCommandContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }
}
