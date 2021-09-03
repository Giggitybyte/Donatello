using System;
using System.Threading.Tasks;
using Qmmands;

namespace Donatello.Interactions.Commands
{
    /// <summary>Base command module with helper methods.</summary>
    public abstract class DiscordCommandModule : ModuleBase<DiscordCommandContext>
    {
        protected ValueTask RespondAsync(in Func<>)
        {
            throw new NotImplementedException();
        }
    }
}
