namespace Donatello.Entity;

using System.Text.Json;
using System.Threading.Tasks;

public class DiscordGuildRole : DiscordGuildEntity
{
    public DiscordGuildRole(DiscordBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    public ValueTask<DiscordGuild> GetGuildAsync() => throw new System.NotImplementedException();
}