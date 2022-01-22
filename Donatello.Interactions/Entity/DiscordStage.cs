namespace Donatello.Interactions.Entity;

using System.Text.Json;

internal sealed class DiscordStage : DiscordEntity
{
    internal DiscordStage(DiscordBot bot, JsonElement json) : base(bot, json) { }
}
