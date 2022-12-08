namespace Donatello.Entity;

using Donatello;
using Donatello.Builder;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>A channel which contains messages sent by users.</summary>
public interface ITextChannel : IChannel
{
    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync(ushort limit = 100);

    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake, ushort limit = 100);

    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake, ushort limit = 100);

    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake, ushort limit = 100);

    public Task<DiscordMessage> SendMessageAsync(MessageBuilder builder);

}