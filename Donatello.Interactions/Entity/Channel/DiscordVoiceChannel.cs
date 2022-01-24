namespace Donatello.Interactions.Entity;

using System.Text.Json;

/// <summary></summary>
public sealed class DiscordVoiceChannel : DiscordChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public int UserLimit => this.Json.GetProperty("user_limit").GetInt32();

    /// <summary></summary>
    public int Bitrate => this.Json.GetProperty("bitrate").GetInt32();
}
