namespace Donatello.Gateway.Entity;

using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public class DiscordTextChannel : DiscordEntity
{
    private MemoryCache _messageCache;

    public DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) 
    { 

    }
}
