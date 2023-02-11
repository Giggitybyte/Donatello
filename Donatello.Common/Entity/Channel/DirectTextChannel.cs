namespace Donatello.Entity;

using Builder;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel that is not associated with a guild which allows for direct messages between two users.</summary>
public class DirectMessageChannel : Channel, ITextChannel
{
    public DirectMessageChannel(Bot bot, JsonElement json) 
        : base(bot, json) 
    { 

    }

    public Task DeleteMessageAsync(Snowflake messageId) => throw new System.NotImplementedException();
    public IAsyncEnumerable<Message> GetMessagesAfterAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();
    public IAsyncEnumerable<Message> GetMessagesAroundAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();
    public IAsyncEnumerable<Message> GetMessagesAsync(ushort limit = 100) => throw new System.NotImplementedException();
    public IAsyncEnumerable<Message> GetMessagesBeforeAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();
    public IAsyncEnumerable<Message> GetPinnedMessagesAsync() => throw new System.NotImplementedException();
    public Task<Message> SendMessageAsync(MessageBuilder builder) => throw new System.NotImplementedException();
}

