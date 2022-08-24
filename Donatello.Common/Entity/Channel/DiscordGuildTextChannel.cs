namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordTextChannel
{
    internal protected DiscordGuildTextChannel(DiscordBot bot, JsonElement json)
        : base(bot, json) { }

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.Json.GetProperty("guild_id").ToSnowflake());
}