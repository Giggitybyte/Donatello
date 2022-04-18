namespace Donatello.Entity;

using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

/// <summary>A channel which contains text messages.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    private MemoryCache _messageCache;

    public DiscordTextChannel(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    public DiscordMessage GetLastMessage()
}

