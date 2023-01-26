namespace Donatello.Entity;

using System.Text.Json;
using System.Threading.Tasks;

public sealed class Role : Entity, IGuildEntity
{
    public Role(Bot bot, JsonElement entityJson) : base(bot, entityJson)
    {

    }

    public Snowflake GuildId => throw new System.NotImplementedException();
    public ValueTask<Guild> GetGuildAsync() => throw new System.NotImplementedException();
}