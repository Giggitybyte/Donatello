namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordSticker : DiscordEntity
{
    public DiscordSticker(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}
