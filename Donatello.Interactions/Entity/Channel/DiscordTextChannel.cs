namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Builder;
using Qommon.Collections;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Text channel.</summary>
public abstract class DiscordTextChannel : DiscordChannel
{
    internal DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(string content)
    {

    }

    /// <summary></summary>
    public Task<DiscordMessage> SendMessageAsync(Action<MessageBuilder> message)
    {

    }

    /// <summary></summary>
    public Task<DiscordChannel> GetLastMessageAsync()
    {

    }

    /// <summary></summary>
    public Task<DiscordMessage> GetMessageAsync(ulong messageId)
    {

    }

    /// <summary></summary>
    public Task<ReadOnlyList<DiscordMessage>> GetMessagesAsync()
    {

    }

    /// <summary></summary>
    public Task DeleteMessageAsync()
    {

    }
}
