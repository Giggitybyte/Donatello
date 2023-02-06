namespace Donatello.Entity;

using Donatello;
using Builder;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>A channel which contains messages sent by users.</summary>
public interface ITextChannel : IChannel
{
    public IAsyncEnumerable<Message> GetMessagesAsync(ushort limit = 100);

    public IAsyncEnumerable<Message> GetMessagesAroundAsync(Snowflake snowflake, ushort limit = 100);

    public IAsyncEnumerable<Message> GetMessagesBeforeAsync(Snowflake snowflake, ushort limit = 100);

    public IAsyncEnumerable<Message> GetMessagesAfterAsync(Snowflake snowflake, ushort limit = 100);

    public IAsyncEnumerable<Message> GetPinnedMessagesAsync();

    public Task<Message> SendMessageAsync(MessageBuilder builder);

    public Task DeleteMessageAsync(Snowflake messageId);
}