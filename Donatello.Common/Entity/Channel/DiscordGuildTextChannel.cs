namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordTextChannel
{
    public DiscordGuildTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

