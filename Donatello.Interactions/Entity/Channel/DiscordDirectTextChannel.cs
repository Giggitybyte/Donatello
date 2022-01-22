namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Extension;
using System.Text.Json;

/// <summary>Private text channel between your bot and a user.</summary>
public sealed class DiscordDirectTextChannel : DiscordTextChannel
{
    internal DiscordDirectTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public DiscordUser Recipient => this.Json.GetProperty("recipients").ToEntityArray<DiscordUser>(this.Bot)[0];
}
