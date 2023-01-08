namespace Donatello.Entity;

using System.Text.Json;

public sealed class DiscordGuildRole : DiscordGuildEntity
{
    internal Role(DiscordBot bot, JsonElement jsonObject)
        : base(bot, jsonObject)
    {

    }
}