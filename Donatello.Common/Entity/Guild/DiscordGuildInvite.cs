namespace Donatello.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordGuildInvite : DiscordGuildEntity
{
    public DiscordGuildInvite(DiscordBot bot, JsonElement entityJson) : base(bot, entityJson)
    {

    }
}

