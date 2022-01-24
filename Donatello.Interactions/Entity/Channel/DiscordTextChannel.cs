namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Model.Builder;
using Qommon.Collections;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel containing messages.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    internal DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(string content)
    {
        throw new NotImplementedException();
    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(Action<MessageBuilder> message)
    {
        throw new NotImplementedException();
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
