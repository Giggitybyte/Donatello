namespace Donatello.Entity;

using Donatello.Entity.Builder;
using Donatello.Rest.Channel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel containing messages.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    private MemoryCache _messageCache;

    internal DiscordTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json)
    {
        _messageCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary></summary>
    public async virtual ValueTask<DiscordMessage> GetMessageAsync(DiscordSnowflake messageId)
    {
        if (_messageCache.TryGetValue(messageId, out DiscordMessage message) is false)
        {
            message = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);

        }

    }

    /// <summary></summary>
    public virtual ValueTask<EntityCollection<DiscordMessage>> GetMessagesAsync();

    /// <summary></summary>
    public ValueTask<EntityCollection<DiscordMessage>> GetMessagesAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var startSnowflake = (ulong)(start.ToUnixTimeMilliseconds() - DiscordSnowflake.DiscordEpoch.ToUnixTimeMilliseconds()) << 22;
        var endSnowflake = (ulong)(end.ToUnixTimeMilliseconds() - DiscordSnowflake.DiscordEpoch.ToUnixTimeMilliseconds()) << 22;

        return GetMessagesAsync(startSnowflake, endSnowflake);
    }

    /// <summary></summary>
    public ValueTask<EntityCollection<DiscordMessage>> GetMessagesAsync(DiscordMessage start, DiscordMessage end)
        => GetMessagesAsync(start.Id, end.Id);

    /// <summary></summary>
    public ValueTask<EntityCollection<DiscordMessage>> GetMessagesAsync(DiscordSnowflake start, DiscordSnowflake end)
    {

    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => builder.Build(jsonWriter));
        return new DiscordMessage(this.Bot, messageJson);
    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(Action<MessageBuilder> builderDelegate)
    {
        var messageBuilder = new MessageBuilder();
        builderDelegate(messageBuilder);

        return SendMessageAsync(messageBuilder);
    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(string content)
        => SendMessageAsync(builder => builder.SetContent(content));

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>Adds or updates an entry in the message cache.</summary>
    protected void UpdateMessageCache(DiscordSnowflake id, DiscordMessage message)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(LogMessageCacheEviction);

        _messageCache.Set(id, message, entryConfig);
        this.Bot.Logger.LogTrace("Updated entry {Id} in user cache", id);

        void LogMessageCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Bot.Logger.LogTrace("Removed stale entry {Id} from user cache", (ulong)key);
    }
}
