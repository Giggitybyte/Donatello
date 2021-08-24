using System;
using Qmmands;

namespace Donatello.Interactions.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DiscordCommandContext : CommandContext
    {
        private readonly string _responseToken;
        private readonly DateTime _tokenExpirationDate;

        internal DiscordCommandContext(string responseToken, IServiceProvider serviceProvider = null) : base(serviceProvider)
        {
            _responseToken = responseToken;
            _tokenExpirationDate = DateTime.Now + TimeSpan.FromMinutes(15);
        }


    }
}
