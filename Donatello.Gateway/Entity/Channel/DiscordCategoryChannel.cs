namespace Donatello.Gateway.Entity;

using System.Text.Json;

/// <summary></summary>
public sealed class DiscordCategoryChannel : DiscordChannel
{
    internal DiscordCategoryChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

