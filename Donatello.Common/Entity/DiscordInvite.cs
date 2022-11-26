namespace Donatello.Entity;

using System.Text.Json;

public class DiscordInvite : DiscordEntity
{
    public DiscordInvite(DiscordBot bot, JsonElement entityJson) 
        : base(bot, entityJson)
    {

    }
}
