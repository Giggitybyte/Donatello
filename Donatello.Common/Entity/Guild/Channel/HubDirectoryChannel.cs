namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Text.Json;

public class HubDirectoryChannel : GuildChannel
{
    public HubDirectoryChannel(JsonElement entityJson, Bot bot) 
        : base(entityJson, bot)
    {
        throw new NotImplementedException();
    }
}
