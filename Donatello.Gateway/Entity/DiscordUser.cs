namespace Donatello.Gateway.Entity;

using System.Text.Json;

public class DiscordUser : DiscordEntity
{
    public DiscordUser(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

