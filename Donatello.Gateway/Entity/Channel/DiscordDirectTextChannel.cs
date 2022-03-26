namespace Donatello.Gateway.Entity;

using System.Text.Json;

/// <summary></summary>
public sealed class DiscordDirectTextChannel : DiscordTextChannel
{
    internal DiscordDirectTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

