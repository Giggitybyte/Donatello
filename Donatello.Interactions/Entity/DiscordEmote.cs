namespace Donatello.Interactions.Entity;

using System.Text.Json;

/// <summary>Custom guild emote.</summary>
public sealed class DiscordEmote : DiscordEntity
{
    public DiscordEmote(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Emote name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>The user which uploaded this emote.</summary>
    public DiscordUser Uploader => new(this.Bot, this.Json.GetProperty("user"));

    /// <summary>Whether this emote is animated.</summary>
    public bool IsAnimated => this.Json.GetProperty("animated").GetBoolean();

    /// <summary>Whether this emote is able to be used.</summary>
    /// <remarks>This can be <see langword="false"/> when the guild loses a boost level.</remarks>
    public bool IsAvailable => this.Json.GetProperty("available").GetBoolean();

    /// <summary>URL to the image for this emote.</summary>
    public string ImageUrl => $"https://cdn.discordapp.com/emojis/{this.Id}.{(this.IsAnimated ? "gif" : "png")}";
}
