namespace Donatello.Core.Entity;

using System.Text.Json;

public sealed class DiscordSticker : DiscordEntity
{
    public DiscordSticker(AbstractBot bot, JsonElement json) : base(bot, json) { }
}
