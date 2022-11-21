namespace Donatello.Entity;

using System.Text.Json;
using System.Threading.Tasks;

public abstract class DiscordGuildEntity : DiscordEntity, IGuildEntity
{
    public DiscordGuildEntity(DiscordBot bot, JsonElement entityJson) 
        : base(bot, entityJson)
    {

    }

    public virtual ValueTask<DiscordGuild> GetGuildAsync() 
        => throw new System.NotImplementedException();
}

