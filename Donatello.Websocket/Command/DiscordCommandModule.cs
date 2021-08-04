using System.Threading.Tasks;
using Qmmands;

namespace Donatello.Websocket.Command
{
    public abstract class DiscordCommandModule : ModuleBase<DiscordCommandContext>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        protected async Task RespondAsync(string text)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageBuilder"></param>
        protected async Task RespondAsync(object messageBuilder)
        {

        }
    }
}
