namespace Donatello.Gateway.Entity;

using System.Text.Json;

public abstract class DiscordChannel : DiscordEntity
{
    internal DiscordChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }


}

