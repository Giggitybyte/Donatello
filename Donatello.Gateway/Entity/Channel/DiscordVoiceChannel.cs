namespace Donatello.Gateway.Entity;

using System.Text.Json;

public class DiscordVoiceChannel : DiscordChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

