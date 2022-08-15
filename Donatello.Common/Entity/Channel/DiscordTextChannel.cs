namespace Donatello.Entity;

using Donatello.Entity.Builder;
using Donatello.Rest.Extension.Endpoint;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Abstract implementation of <see cref="ITextChannel"/></summary>
public abstract class DiscordTextChannel : DiscordChannel, ITextChannel
{
    internal DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        this.MessageCache = new EntityCache<DiscordMessage>();
        
    }

    /// <summary></summary>
    public async ValueTask<DiscordMessage> GetMessageAsync(DiscordSnowflake messageId)
    {
        if (this.MessageCache.TryGetEntity(messageId, out DiscordMessage message) is false)
        {
            var messageJson = await this.Bot.RestClient.GetChannelMessageAsync(this.Id, messageId);
            message = new DiscordMessage(this.Bot, messageJson);

            this.MessageCache.Add(message);
        }

        return message;
    }

    /// <summary>Cached message instances.</summary>
    public EntityCache<DiscordMessage> MessageCache { get; private init; }

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

        foreach (var message in messages)
            this.MessageCache.Add(message);

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
}
