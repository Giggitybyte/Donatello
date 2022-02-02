namespace Donatello.Interactions.Entity.Channel;

using Donatello.Interactions.Payload;
using Donatello.Rest.Endpoint;
using Qommon.Collections;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel containing messages.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    internal DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Message content cannot be longer than 2,000 characters.", nameof(content));

        var response = await this.Bot.HttpClient.CreateMessageAsync(this.Id, (json) => json.WriteString("content", content));
        var message = response.Payload.ToEntity<DiscordMessage>(this.Bot);

        return message;
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(Action<MessageWriter> message)
    {
        var builder = new MessageWriter();
        message(builder);

        var response = await this.Bot.HttpClient.CreateMessageAsync(this.Id, builder.WritePayload, builder.Attachments);
        return new DiscordMessage(this.Bot, response.Payload);
    }

    /// <summary></summary>
    public Task<DiscordChannel> GetLastMessageAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public Task<DiscordMessage> GetMessageAsync(ulong messageId)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public Task<ReadOnlyList<DiscordMessage>> GetMessagesAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {
        throw new NotImplementedException();
    }
}
