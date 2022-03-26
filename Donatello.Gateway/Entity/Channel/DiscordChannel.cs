namespace Donatello.Gateway.Entity;

using System.Text.Json;

/// <summary></summary>
public abstract class DiscordChannel : DiscordEntity
{
    protected DiscordChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

