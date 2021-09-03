using System.Text.Json;

namespace Donatello.Interactions.Entities
{
    public sealed class DiscordSticker : DiscordEntity
    {
        public DiscordSticker(JsonElement json) : base(json) { }
    }
}
