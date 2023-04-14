namespace Donatello.Common.Entity.Guild;

using System.Text.Json;
using System.Threading.Tasks;

public sealed class Role : GuildEntity
{
    public Role(Bot bot, JsonElement entityJson) : base(entityJson, bot)
    {

    }
}