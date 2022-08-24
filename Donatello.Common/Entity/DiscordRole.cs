namespace Donatello.Entity;

using System.Text.Json;

public class DiscordRole : DiscordEntity
{
    public DiscordRole(DiscordBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }
}