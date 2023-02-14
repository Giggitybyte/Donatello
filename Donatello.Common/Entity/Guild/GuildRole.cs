namespace Donatello.Common.Entity.Guild;

using System.Text.Json;

public sealed class Role : Entity, IGuildEntity
{
    public Role(Bot bot, JsonElement entityJson) : base(bot, entityJson)
    {

    }

    public Snowflake GuildId => throw new System.NotImplementedException();
}