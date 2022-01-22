namespace Donatello.Interactions.Entity;

using System.Text.Json;

/// <summary></summary>
public sealed class DiscordVoiceChannel : DiscordChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}
