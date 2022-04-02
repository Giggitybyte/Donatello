namespace Donatello.Interaction.Command.Module;

using System;
using System.Threading.Tasks;
using Qmmands;

/// <summary>Base command module with helper methods.</summary>
public abstract class DiscordCommandModule : ModuleBase<DiscordCommandContext>
{
    protected ValueTask RespondAsync()
    {
        throw new NotImplementedException();
    }
}
