namespace Donatello.Common.Entity.Guild.Channel;

using System.Text.Json;

/// <summary></summary>
public class GuildNewsChannel : GuildTextChannel
{
    public GuildNewsChannel(JsonElement json) 
        : base(json) 
    { 
    }

    public GuildNewsChannel(JsonElement entityJson, Snowflake guildId)
        : base(entityJson, guildId)
    {
    }
}

