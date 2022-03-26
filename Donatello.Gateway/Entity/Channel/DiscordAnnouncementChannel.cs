namespace Donatello.Gateway.Entity;

using System.Text.Json;

/// <summary></summary>
public sealed class DiscordAnnouncementChannel : DiscordGuildTextChannel
{
    internal DiscordAnnouncementChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

