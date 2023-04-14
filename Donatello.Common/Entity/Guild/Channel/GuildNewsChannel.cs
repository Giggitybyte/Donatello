namespace Donatello.Common.Entity.Guild.Channel;

using System.Text.Json;

/// <summary></summary>
public class GuildNewsChannel : GuildTextChannel
{
    public GuildNewsChannel(JsonElement json, Bot bot) : base(json, bot) { }
    public GuildNewsChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }
}

