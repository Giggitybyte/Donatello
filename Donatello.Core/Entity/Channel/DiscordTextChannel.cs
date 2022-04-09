namespace Donatello.Entity;

using Donatello.Entity.Builder;
using Donatello.Rest.Channel;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel containing text messages.</summary>
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
        var messageId = this.Json.GetProperty("last_message_id").ToUInt64();
        return GetMessageAsync(messageId);
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Message content cannot be longer than 2,000 characters.", nameof(content));

        var messageJson = await this.Bot.RestClient.CreateMessageAsync(this.Id, (json) =>
        {
            json.WriteString("content", content);
        });

        return new DiscordMessage(this.Bot, messageJson);
    }

    /// <summary></summary>
    public async Task<DiscordMessage> SendMessageAsync(Action<MessageBuilder> message)
    {
        var builder = new MessageBuilder();
        message(builder);

        var response = await this.Bot.RestClient.CreateMessageAsync(this.Id, json => builder.WriteJson(json));
        return new DiscordMessage(this.Bot, response);
    }

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {
        throw new NotImplementedException();
    }
}
