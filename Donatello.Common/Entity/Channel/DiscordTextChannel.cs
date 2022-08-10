namespace Donatello.Entity;

using Donatello.Entity.Builder;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Abstract implementation of <see cref="ITextChannel"/></summary>
public abstract class DiscordTextChannel : DiscordChannel, ITextChannel
{
    private MemoryCache _messageCache;

    internal DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        _messageCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary></summary>
    public async ValueTask<DiscordMessage> GetMessageAsync(DiscordSnowflake messageId)
    {
        if (_messageCache.TryGetValue(messageId, out DiscordMessage message) is false)
        {
            var messageJson = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);
            message = new DiscordMessage(this.Bot, messageJson);

            UpdateMessageCache(messageId, message);
        }

        return message;
    }

    /// <summary></summary>
    public Task<EntityCollection<DiscordMessage>> GetMessagesAsync()
        => GetMessagesCoreAsync();

    /// <summary></summary>
    public Task<EntityCollection<DiscordMessage>> GetMessagesAroundAsync(DiscordSnowflake snowflake)
        => GetMessagesCoreAsync(("around", snowflake.ToString()));

    /// <summary></summary>
    public Task<EntityCollection<DiscordMessage>> GetMessagesBeforeAsync(DiscordSnowflake snowflake)
        => GetMessagesCoreAsync(("before", snowflake.ToString()));

    /// <summary></summary>
    public Task<EntityCollection<DiscordMessage>> GetMessagesAfterAsync(DiscordSnowflake snowflake)
        => GetMessagesCoreAsync(("after", snowflake.ToString()));

    private async Task<EntityCollection<DiscordMessage>> GetMessagesCoreAsync((string key, string value) query = default)
    {
        var messageArray = await this.Bot.RestClient.GetChannelMessagesAsync(this.Id, query, ("limit", "100"));
        var messages = messageArray.EnumerateArray().Select(messageJson => new DiscordMessage(this.Bot, messageJson));

        return new EntityCollection<DiscordMessage>(messages);
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => builder.ConstructJson(jsonWriter));
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
    protected internal void UpdateMessageCache(DiscordSnowflake id, DiscordMessage message)
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
