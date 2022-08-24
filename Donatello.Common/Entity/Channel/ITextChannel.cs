namespace Donatello.Entity;

using Donatello.Entity.Builder;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITextChannel : IChannel
{
    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync();

    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake);

    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake);

    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake);

    public Task<DiscordMessage> SendMessageAsync(MessageBuilder builder);

}

