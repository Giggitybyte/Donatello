namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGuildVoiceChannel : DiscordGuildChannel, IVoiceChannel, ITextChannel
{
    public DiscordGuildVoiceChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) { }
}

