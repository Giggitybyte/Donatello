namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordVoiceChannel : DiscordTextChannel
{
    public DiscordVoiceChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

