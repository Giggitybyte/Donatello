namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordGuildVoiceChannel : DiscordGuildTextChannel, IVoiceChannel
{
    public DiscordGuildVoiceChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json) { }


}

