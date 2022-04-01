namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordSticker : DiscordEntity
{
    public DiscordSticker(Bot bot, JsonElement json) : base(bot, json) { }
}
