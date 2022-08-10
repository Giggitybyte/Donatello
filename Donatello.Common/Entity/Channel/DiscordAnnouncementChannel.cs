namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordAnnouncementChannel : DiscordGuildTextChannel
{
    public DiscordAnnouncementChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

