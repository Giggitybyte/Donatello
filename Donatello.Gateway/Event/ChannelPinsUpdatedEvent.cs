namespace Donatello.Gateway.Event;

using Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Entity.Guild;
using Common.Entity.Guild.Channel;
using Common.Entity.Message;

/// <summary>Dispatched when a message is pinned or unpinned in a text channel.</summary>
public class ChannelPinsUpdatedEvent : ShardEvent
{
    /// <summary>ID of the updated channel.</summary>
    public Snowflake ChannelId { get; internal init; }

    /// <summary>Guild ID for the updated channel.</summary>
    public Snowflake GuildId { get; internal init; }

    /// <summary>Attempts to </summary>
    public ValueTask<Guild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.GuildId);

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