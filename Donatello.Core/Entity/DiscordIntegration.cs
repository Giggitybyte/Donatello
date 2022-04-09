namespace Donatello.Entity;

using System.Text.Json;

public class DiscordIntegration : DiscordEntity
{
    public DiscordIntegration(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

