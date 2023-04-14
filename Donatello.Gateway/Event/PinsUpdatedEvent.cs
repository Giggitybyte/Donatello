namespace Donatello.Gateway.Event;

using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Entity.Guild.Channel;
using Common.Entity.Message;

/// <summary>Dispatched when a message is pinned or unpinned in a text channel.</summary>
public class PinsUpdatedEvent : GuildEvent
{
    /// <summary>ID of the updated channel.</summary>
    public Snowflake ChannelId { get; internal init; }

    /// <inheritdoc cref="Bot.GetChannelAsync(Snowflake)"/>
    public ValueTask<GuildTextChannel> GetChannelAsync()
        => this.Bot.GetChannelAsync<GuildTextChannel>(this.ChannelId);

    /// <summary></summary>
    public async IAsyncEnumerable<Message> GetPinnedMessagesAsync()
    {
        var channel = await this.GetChannelAsync();
        await foreach (var message in channel.GetPinnedMessagesAsync())
            yield return message;
    }
}