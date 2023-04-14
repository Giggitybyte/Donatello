namespace Donatello.Common.Entity.Guild;

using System.Text.Json;

public class GuildInvite : GuildEntity
{
    public GuildInvite(JsonElement entityJson, Bot bot) 
        : base(entityJson, bot)
    {

    }
}
