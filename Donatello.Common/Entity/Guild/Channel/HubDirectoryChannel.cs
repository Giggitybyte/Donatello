namespace Donatello.Entity;

using System.Text.Json;

public class HubDirectoryChannel : GuildChannel
{
    public HubDirectoryChannel(Bot bot, JsonElement entityJson) 
        : base(bot, entityJson)
    {

    }
}
