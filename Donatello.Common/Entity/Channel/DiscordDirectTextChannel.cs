namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordDirectTextChannel : DiscordTextChannel
{
    public DiscordDirectTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

