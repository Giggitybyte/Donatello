namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordVoiceChannel : DiscordTextChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

