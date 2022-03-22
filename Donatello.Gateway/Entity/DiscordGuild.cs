namespace Donatello.Gateway.Entity;

using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public class DiscordGuild : DiscordEntity
{
    private MemoryCache _memberCache;

    public DiscordGuild(DiscordBot bot, JsonElement json) : base(bot, json) 
    { 
        _me
    }
}

