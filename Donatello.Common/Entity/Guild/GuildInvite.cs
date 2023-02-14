namespace Donatello.Common.Entity.Guild;

using System.Text.Json;

public class GuildInvite : Entity
{
    public GuildInvite(Bot bot, JsonElement entityJson) 
        : base(bot, entityJson)
    {

    }
}
