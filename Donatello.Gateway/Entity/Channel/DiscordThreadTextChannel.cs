namespace Donatello.Gateway.Entity;

using System.Text.Json;

public class DiscordThreadTextChannel : DiscordGuildTextChannel
{
    internal DiscordThreadTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

