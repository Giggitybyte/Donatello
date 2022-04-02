namespace Donatello.Entity;

using System.Text.Json;

internal sealed class DiscordStage : DiscordEntity
{
    internal DiscordStage(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}
