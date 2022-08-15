namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordVoiceChannel : DiscordChannel, ITextChannel, IVoiceChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

