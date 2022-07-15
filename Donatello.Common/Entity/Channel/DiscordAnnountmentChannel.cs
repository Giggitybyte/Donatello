namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordAnnountmentChannel : DiscordGuildTextChannel
{
    public DiscordAnnountmentChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }
}

