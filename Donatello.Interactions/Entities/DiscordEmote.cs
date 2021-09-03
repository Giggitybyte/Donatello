using System.Text.Json;

namespace Donatello.Interactions.Entities
{
    /// <summary>Custom guild emote.</summary>
    public sealed class DiscordEmote : DiscordEntity
    {
        public DiscordEmote(JsonElement json) : base(json) { }

        /// <summary>Emote name.</summary>
        public string Name 
            => Json.GetProperty("name").GetString();

        /// <summary>The user which uploaded this emote.</summary>
        public DiscordUser Uploader 
            => new(Json.GetProperty("user"));

        /// <summary>Whether this emote is animated.</summary>
        public bool IsAnimated 
            => Json.GetProperty("animated").GetBoolean();

        /// <summary>Whether this emote is able to be used.</summary>
        /// <remarks>This can be <see langword="false"/> when the guild loses a boost level.</remarks>
        public bool IsAvailable 
            => Json.GetProperty("available").GetBoolean();

        /// <summary>URL to the image for this emote.</summary>
        public string ImageUrl 
            => $"https://cdn.discordapp.com/emojis/{this.Id}.{(this.IsAnimated ? "gif" : "png")}";
    }
}
