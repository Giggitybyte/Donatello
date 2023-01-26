namespace Donatello.Entity;

using System.Text.Json;

public class GuildStageChannel : GuildVoiceChannel
{
    public GuildStageChannel(Bot bot, JsonElement json) 
        : base(bot, json)
    {

    }
}
