namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordRole : DiscordEntity
{
    public DiscordRole(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }
}