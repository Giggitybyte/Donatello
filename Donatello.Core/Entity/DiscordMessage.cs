namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A message sent in a channel within Discord.</summary>
public sealed class DiscordMessage : DiscordEntity
{
    public DiscordMessage(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    public bool SentFromUser(out DiscordUser user)
    {
        if (this.Json.TryGetProperty("guild_id"))
    }

    public bool SentFromWebhook(out)

    /// <summary></summary>
    public ValueTask<DiscordTextChannel> GetChannelAsync()
        => this.Bot.GetChannelAsync<DiscordTextChannel>(this.Json.GetProperty("channel_id").ToUInt64());
}