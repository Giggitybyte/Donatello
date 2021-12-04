namespace Donatello.Interactions.Entity;

using System.Text.Json;

public sealed class DiscordSticker : DiscordEntity
{
    public DiscordSticker(DiscordBot bot, JsonElement json) : base(bot, json) { }
}
