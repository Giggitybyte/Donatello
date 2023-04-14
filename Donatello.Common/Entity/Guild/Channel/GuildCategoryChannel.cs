namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Text.Json;

public class GuildCategoryChannel : GuildChannel
{
    public GuildCategoryChannel(JsonElement entityJson, Bot bot) : base(entityJson, bot) { }
    public GuildCategoryChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }

    public IAsyncEnumerable<GuildChannel> GetContainedChannels()
    {
        throw new NotImplementedException();
    }
}
