namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGroupTextChannel : DiscordTextChannel
{
    public DiscordGroupTextChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) 
    { 

    }
}

