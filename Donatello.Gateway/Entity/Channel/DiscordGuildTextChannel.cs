namespace Donatello.Gateway.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordTextChannel
{
    internal DiscordGuildTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

