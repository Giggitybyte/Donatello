namespace Donatello.Entity;

using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordTextChannel, IGuildChannel
{
    internal protected DiscordGuildTextChannel(DiscordBot bot, JsonElement json)
        : base(bot, json)
    {

    }

    public ValueTask<DiscordGuild> GetGuildAsync() => throw new System.NotImplementedException();
}