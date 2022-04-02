namespace Donatello.Entity;

using System.Text.Json;

/// <summary>Private text channel between your bot and a user.</summary>
public sealed class DiscordDirectTextChannel : DiscordTextChannel
{
    internal DiscordDirectTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>The user who will receive your messages.</summary>
    public DiscordUser Recipient => this.Json.GetProperty("recipients").ToEntityArray<DiscordUser>(this.Bot)[0];
}
