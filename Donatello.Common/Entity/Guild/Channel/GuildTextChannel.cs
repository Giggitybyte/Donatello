namespace Donatello.Entity;

using Donatello.Builder;
using Donatello.Rest.Extension.Endpoint;
using Donatello.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class GuildTextChannel : GuildChannel, ITextChannel
{
    internal protected GuildTextChannel(Bot bot, JsonElement json)
        : base(bot, json)
    {
        this.MessageCache = new EntityCache<Message>();
        this.ThreadCache = new EntityCache<GuildThreadChannel>();
    }

    /// <summary>Cached message instances.</summary>
    public EntityCache<Message> MessageCache { get; private init; }

    /// <summary>Cached thread channel instances.</summary>
    public EntityCache<GuildThreadChannel> ThreadCache { get; private init; }

    /// <summary></summary>
    public async ValueTask<Message> GetMessageAsync(Snowflake messageId)
    {
        if (this.MessageCache.TryGet(messageId, out Message message) is false)
        {
            var messageJson = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);
            message = new Message(this.Bot, messageJson);

            this.MessageCache.Add(messageId, message);
        }

        return message;
    }

    /// <summary></summary>
    public IAsyncEnumerable<Message> GetMessagesAsync(ushort limit = 100)
        => this.GetMessagesCoreAsync(limit);

    /// <summary></summary>
    public IAsyncEnumerable<Message> GetMessagesAroundAsync(Snowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "around");

    /// <summary></summary>
    public IAsyncEnumerable<Message> GetMessagesBeforeAsync(Snowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "before");

    /// <summary></summary>
    public IAsyncEnumerable<Message> GetMessagesAfterAsync(Snowflake snowflake, ushort limit = 100)
        => this.GetMessagesCoreAsync(limit, snowflake, "after");

    private async IAsyncEnumerable<Message> GetMessagesCoreAsync(ushort limit, Snowflake snowflake = null, string timeframe = null)
    {
        var messages = snowflake is null | timeframe is null
            ? this.Bot.RestClient.GetChannelMessagesAsync(this.Id, ("limit", limit.ToString()))
            : this.Bot.RestClient.GetChannelMessagesAsync(this.Id, (timeframe, snowflake.ToString()), ("limit", limit.ToString()));

        await foreach (var message in messages.Select(json => new Message(this.Bot, json)))
        {
            this.MessageCache.Add(message.Id, message);
            yield return message;
        }
    }

    /// <summary></summary>
    public async IAsyncEnumerable<Message> GetPinnedMessagesAsync()
    {
        var messages = this.Bot.RestClient.GetPinnedMessagesAsync(this.Id)
            .Select(messageJson => new Message(this.Bot, messageJson));

        await foreach (var message in messages)
        {
            this.MessageCache.Add(message);
            yield return message;
        }
    }

    /// <summary></summary>
    public async Task<Message> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => builder.Json.WriteTo(jsonWriter), builder.Files);
        var message = new Message(this.Bot, messageJson);
        this.MessageCache.Add(message.Id, message);

        return message;
    }

    /// <summary></summary>
    public Task<Message> SendMessageAsync(Action<MessageBuilder> builderDelegate)
    {
        var messageBuilder = new MessageBuilder();
        builderDelegate(messageBuilder);

        return this.SendMessageAsync(messageBuilder);
    }

    /// <summary></summary>
    public Task<Message> SendMessageAsync(string content)
        => this.SendMessageAsync(builder => builder.SetContent(content));

    /// <summary></summary>
    public Task DeleteMessageAsync(Snowflake messageId)
    {
        throw new NotImplementedException();
    }
}