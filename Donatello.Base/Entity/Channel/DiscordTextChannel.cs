namespace Donatello.Entity;

using Donatello.Entity.Builder;
using Donatello.Extension.Internal;
using Donatello.Rest.Channel;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel containing messages.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    internal DiscordTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public abstract ValueTask<DiscordMessage> GetMessageAsync(ulong messageId);

    /// <summary></summary>
    public abstract ValueTask<EntityCollection<DiscordMessage>> GetMessagesAsync();

    /// <summary></summary>
    public ValueTask<DiscordMessage> GetLastMessageAsync()
    {
        var messageId = this.Json.GetProperty("last_message_id").ToSnowflake();
        return this.GetMessageAsync(messageId);
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Message content cannot be longer than 2,000 characters.", nameof(content));

        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, (jsonWriter) => jsonWriter.WriteString("content", content));

        return new DiscordMessage(this.Bot, messageJson);
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(MessageBuilder messageBuilder)
    {
        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, jsonWriter => messageBuilder.Build(jsonWriter));
        return new DiscordMessage(this.Bot, messageJson);
    }

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {
        throw new NotImplementedException();
    }
}
