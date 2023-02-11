namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class GuildNewsChannel : GuildTextChannel
{
    public GuildNewsChannel(Bot bot, JsonElement json) 
        : base(bot, json) 
    { 
    }

    public GuildNewsChannel(Bot bot, JsonElement entityJson, Snowflake guildId)
        : base(bot, entityJson, guildId)
    {
    }
}

