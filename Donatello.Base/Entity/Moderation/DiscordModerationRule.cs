namespace Donatello.Entity;

using Donatello.Enumeration;
using Donatello.Extension.Internal;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Auto moderation trigger based on a criteria.</summary>
public class DiscordModerationRule : DiscordEntity
{
    public DiscordModerationRule(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    /// <summary>The rule name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>The type of content which can trigger the rule.</summary>
    public ModerationEventType Type => (ModerationEventType)this.Json.GetProperty("type").GetUInt16();

    /// <summary>Fetches the user who first created this rule.</summary>
    public async ValueTask<DiscordUser> GetCreatorAsync()
        => await this.Bot.GetUserAsync(this.Json.GetProperty("creator_id").ToSnowflake());

    /// <summary>Fetches the guild which this rule belongs to.</summary>
    public async ValueTask<DiscordGuild> GetGuildAsync()
        => await this.Bot.GetGuildAsync(this.Json.GetProperty("guild_id").ToSnowflake());
}

