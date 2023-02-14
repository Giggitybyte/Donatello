namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Text.Json;

public class GuildCategoryChannel : GuildChannel
{
    public GuildCategoryChannel(JsonElement entityJson) : base(bot, entityJson)
    {
    }

    public GuildCategoryChannel(JsonElement entityJson, Snowflake guildId)
        : base(entityJson, guildId)
    {
    }

    public IAsyncEnumerable<GuildChannel> GetContainedChannels()
    {
        throw new NotImplementedException();
    }
}
