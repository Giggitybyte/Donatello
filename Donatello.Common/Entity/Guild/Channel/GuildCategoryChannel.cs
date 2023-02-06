namespace Donatello.Entity;

using System;
using System.Collections.Generic;
using System.Text.Json;

public class GuildCategoryChannel : GuildChannel
{
    public GuildCategoryChannel(Bot bot, JsonElement entityJson) : base(bot, entityJson)
    {
    }

    public GuildCategoryChannel(Bot bot, JsonElement entityJson, Snowflake guildId)
        : base(bot, entityJson, guildId)
    {
    }

    public IAsyncEnumerable<GuildChannel> GetContainedChannels()
    {
        throw new NotImplementedException();
    }
}
