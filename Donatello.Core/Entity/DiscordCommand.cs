namespace Donatello.Entity;

using Donatello.Enumeration;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class DiscordCommand : DiscordEntity
{
    public DiscordCommand(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Command interaction method.</summary>
    public CommandType Type
    {
        get
        {
            if (this.Json.TryGetProperty("type", out var property))
                return (CommandType)property.GetInt32();
            else
                return CommandType.Slash;
        }
    }

    public async ValueTask<DiscordGuild> GetGuildAsync()
    {
        if (this.Json.TryGetProperty("guild_id", out var property))
        {

        }
    }


}
