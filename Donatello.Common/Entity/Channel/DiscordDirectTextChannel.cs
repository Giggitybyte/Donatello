namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordDirectTextChannel : DiscordTextChannel
{
    public DiscordDirectTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

