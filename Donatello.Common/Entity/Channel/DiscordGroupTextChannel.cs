namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGroupTextChannel : DiscordTextChannel
{
    public DiscordGroupTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

