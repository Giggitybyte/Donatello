namespace Donatello.Interactions.Entity;

using Donatello.Rest.Endpoint;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A text channel that can crosspost to following channels.</summary>
public sealed class DiscordAnnouncementChannel : DiscordGuildTextChannel
{
    internal DiscordAnnouncementChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Send a message in this channel to all following channels.</summary>
    public Task CrosspostMessageAsync(DiscordMessage message)
        => this.Bot.HttpClient.CrosspostMessageAsync(message.ChannelId, message.Id);

    /// <summary>Adds the provided <paramref name="channel"/> as a follower to this channel.</summary>
    /// <remarks>Requires the <c>MANAGE_WEBHOOKS</c> permission in the <paramref name="channel"/>.</remarks>
    public Task AddFollowerAsync(DiscordTextChannel channel)
        => this.Bot.HttpClient.FollowNewsChannelAsync(this.Id, channel.Id);
}
