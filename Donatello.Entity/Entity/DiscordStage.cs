namespace Donatello.Core.Entity;

using System.Text.Json;

internal sealed class DiscordStage : DiscordEntity
{
    internal DiscordStage(AbstractBot bot, JsonElement json) : base(bot, json) { }
}
