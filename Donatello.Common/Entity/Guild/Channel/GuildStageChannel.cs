namespace Donatello.Common.Entity.Guild.Channel;

using System.Text.Json;

public class GuildStageChannel : GuildVoiceChannel
{
    public GuildStageChannel(JsonElement json, Bot bot) : base(json, bot) { }
    public GuildStageChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }
}
