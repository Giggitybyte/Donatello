namespace Donatello.Entity;

using Donatello.Entity.Builder;
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
        this.MessageCache = new ObjectCache<DiscordMessage>();
    }

    /// <summary>Cached message instances.</summary>
    public ObjectCache<DiscordMessage> MessageCache { get; private init; }

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
    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync()
        => this.GetMessagesCoreAsync();

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake)
        => this.GetMessagesCoreAsync(("around", snowflake.ToString()));

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake)
        => this.GetMessagesCoreAsync(("before", snowflake.ToString()));

    /// <summary></summary>
    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake)
        => this.GetMessagesCoreAsync(("after", snowflake.ToString()));

    private async IAsyncEnumerable<DiscordMessage> GetMessagesCoreAsync((string key, string value) query = default)
    {
        this.MessageCache.Clear();

        var messageArray = await this.Bot.RestClient.GetChannelMessagesAsync(this.Id, query, ("limit", "100"));
        var messages = messageArray.EnumerateArray().Select(messageJson => new DiscordMessage(this.Bot, messageJson));

        foreach (var message in messages)
        {
            this.MessageCache.Add(message.Id, message);
            yield return message;
        }
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => builder.ConstructJson(jsonWriter));
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
