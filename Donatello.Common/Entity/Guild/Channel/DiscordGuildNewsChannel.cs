namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGuildNewsChannel : DiscordGuildTextChannel
{
    public DiscordGuildNewsChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) { }
}

