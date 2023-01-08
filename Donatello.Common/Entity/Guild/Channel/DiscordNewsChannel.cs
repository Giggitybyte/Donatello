namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordNewsChannel : DiscordGuildTextChannel
{
    public DiscordNewsChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) { }
}

