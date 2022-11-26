namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordGuildRole : DiscordGuildEntity
{
    internal DiscordGuildRole(DiscordBot bot, JsonElement jsonObject) 
        : base(bot, jsonObject) 
    {

    }
}