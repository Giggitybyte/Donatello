namespace Donatello.Entity;

using Donatello;
using Donatello.Builder;
using Donatello.Cache;
using Donatello.Rest.Extension.Endpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Abstract implementation of <see cref="ITextChannel"/></summary>
public abstract class DiscordTextChannel : DiscordChannel, ITextChannel
{
    internal protected DiscordTextChannel(DiscordBot bot, JsonElement json) 
        : base(bot, json)
    {
        this.MessageCache = new EntityCache<DiscordMessage>();
    }

    /// <summary>Cached message instances.</summary>
    public EntityCache<DiscordMessage> MessageCache { get; private init; }

    /// <summary></summary>
    public async ValueTask<DiscordMessage> GetMessageAsync(DiscordSnowflake messageId)
    {
        if (this.MessageCache.Contains(messageId, out DiscordMessage message) is false)
        {
            var messageJson = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);
            message = new DiscordMessage(this.Bot, messageJson);

            this.MessageCache.Add(messageId, message);
        }

        return message;
    }

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync(ushort limit = 100)
        => this.GetMessagesCoreAsync(limit);

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "around");

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "before");

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "after");

    private async IAsyncEnumerable<DiscordMessage> GetMessagesCoreAsync(ushort limit, DiscordSnowflake snowflake = null, string timeframe = null)
    {
        var messages = snowflake is null | timeframe is null
            ? this.Bot.RestClient.GetChannelMessagesAsync(this.Id, ("limit", limit.ToString()))
            : this.Bot.RestClient.GetChannelMessagesAsync(this.Id, (timeframe, snowflake.ToString()), ("limit", limit.ToString()));

        await foreach (var message in messages.Select(json => new DiscordMessage(this.Bot, json)))
        {
            this.MessageCache.Add(message.Id, message);
            yield return message;
        }
    }

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordMessage> GetPinnedMessagesAsync()
    {
        var messages = this.Bot.RestClient.GetPinnedMessagesAsync(this.Id)
            .Select(messageJson => new DiscordMessage(this.Bot, messageJson));

        await foreach (var message in messages)
        {
            this.MessageCache.Add(message);
            yield return message;
        }
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.SendRequestAsync()
        var message = new DiscordMessage(this.Bot, messageJson);
        this.MessageCache.Add(message.Id, message);

        return message;
    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(Action<MessageBuilder> builderDelegate)
    {
        var messageBuilder = new MessageBuilder();
        builderDelegate(messageBuilder);

        return this.SendMessageAsync(messageBuilder);
    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(string content)
        => this.SendMessageAsync(builder => builder.SetContent(content));

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {
        throw new NotImplementedException();
    }
}
