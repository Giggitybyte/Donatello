namespace Donatello.Interactions.Entity;

using System.Text.Json;

public sealed class DiscordStageChannel : DiscordChannel
{
    internal DiscordStageChannel(DiscordBot bot, JsonElement json) : base(bot, json)    {    }
}
