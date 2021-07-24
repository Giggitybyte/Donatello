using System;
using System.Threading.Tasks;
using Donatello.Websocket.Bot;

namespace Donatello.Websocket.Test
{
    public class Bot
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            var discordBot = new DiscordBot("NzUzNzQ5NjE3NDA0OTM2MjMz.X1quCA.XgtstzxPn7lkm8iEVbzwOSruAgM");
            await discordBot.StartAsync();

            await Task.Delay(-1);
            
        }
    }
}
