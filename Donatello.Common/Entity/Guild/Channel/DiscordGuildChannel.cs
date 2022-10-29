namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Abstract implemetation of <see cref="IChannel"/> and <see cref="IGuildEntity"/>.</summary>
public abstract class DiscordGuildChannel : DiscordChannel, IGuildEntity
{
    public DiscordGuildChannel(DiscordBot bot, JsonElement json)
        : base(bot, json) { }

    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.Json.GetProperty("guild_id").ToSnowflake());
}