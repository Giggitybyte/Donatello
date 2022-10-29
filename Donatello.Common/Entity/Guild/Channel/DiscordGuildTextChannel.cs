namespace Donatello.Entity;

using Donatello.Entity.Builder;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Implementation of <see cref="ITextChannel"/> and <see cref="IGuildEntity"/>.</summary>
public class DiscordGuildTextChannel : DiscordGuildChannel, ITextChannel
{
    internal protected DiscordGuildTextChannel(DiscordBot bot, JsonElement json)
        : base(bot, json) { }

    public ValueTask<DiscordGuild> GetGuildAsync() => throw new System.NotImplementedException();
    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake) => throw new System.NotImplementedException();
    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake) => throw new System.NotImplementedException();
    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync() => throw new System.NotImplementedException();
    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake) => throw new System.NotImplementedException();
    public Task<DiscordMessage> SendMessageAsync(MessageBuilder builder) => throw new System.NotImplementedException();
}