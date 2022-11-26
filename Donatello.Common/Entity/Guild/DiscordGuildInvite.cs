namespace Donatello.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordGuildInvite : DiscordEntity, IGuildEntity
{
    public DiscordGuildInvite(DiscordBot bot, JsonElement entityJson) : base(bot, entityJson)
    {

    }

    public ValueTask<DiscordGuild> GetGuildAsync() => throw new NotImplementedException();
}

