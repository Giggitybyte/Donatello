namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordRole : DiscordEntity
{
    public DiscordRole(DiscordBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }
}