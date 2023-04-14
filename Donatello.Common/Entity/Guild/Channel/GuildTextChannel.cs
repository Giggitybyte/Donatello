namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Builder;
using Common.Entity.Channel;
using Donatello.Rest.Extension.Endpoint;
using Message;

/// <summary></summary>
public class GuildTextChannel : GuildChannel, ITextChannel
{
    public GuildTextChannel(JsonElement json, Bot bot) : base(json, bot) { }
    public GuildTextChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }

    /// <summary></summary>
    public async ValueTask<Message> GetMessageAsync(Snowflake messageId)
    {
        if (this.Bot.MessageCache.TryGetEntry(messageId, out JsonElement messageJson) is false)
        {
            messageJson = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);
            this.Bot.MessageCache.AddOrUpdate(messageJson);
        }

        return new Message(messageJson, this.Bot);
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

        await foreach (var messageJson in messages)
        {
            this.Bot.MessageCache.AddOrUpdate(messageJson);
            yield return new Message(messageJson, this.Bot);
        }
    }

    /// <summary></summary>
    public async IAsyncEnumerable<Message> GetPinnedMessagesAsync()
    {
        var messages = this.Bot.RestClient.GetPinnedMessagesAsync(this.Id);
        await foreach (var message in messages)
        {
            this.Bot.MessageCache.AddOrUpdate(message);
            yield return new Message(message, this.Bot);
        }
    }

    /// <summary></summary>
    public async Task<Message> SendMessageAsync(MessageBuilder builder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => builder.Json.WriteTo(jsonWriter), builder.Files);
        this.Bot.MessageCache.AddOrUpdate(messageJson);

        return new Message(messageJson, this.Bot);
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