namespace Donatello.Entity;

using System.Text.Json;

/// <summary>A channel which is not associated with a guild that allows for direct messages between two users.</summary>
public class DiscordDirectTextChannel : DiscordTextChannel
{
    public DiscordDirectTextChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) 
    { 

    }
}

