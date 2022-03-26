namespace Donatello.Gateway.Entity;

using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public abstract class DiscordTextChannel : DiscordChannel
{
    private MemoryCache _messageCache;

    protected DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) 
    { 

    }
}
