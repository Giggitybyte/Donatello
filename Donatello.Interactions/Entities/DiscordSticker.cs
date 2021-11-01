namespace Donatello.Interactions.Entities;

using System.Text.Json;

public sealed class DiscordSticker : DiscordEntity
{
    public DiscordSticker(JsonElement json) : base(json) { }
}
