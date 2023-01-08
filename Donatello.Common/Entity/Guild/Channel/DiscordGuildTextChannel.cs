namespace Donatello.Entity;

using Donatello.Builder;
using Donatello.Cache;
using Donatello.Rest.Extension.Endpoint;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordGuildChannel, ITextChannel
{
    internal protected DiscordGuildTextChannel(DiscordBot bot, JsonElement json)
        : base(bot, json)
    {

    }

    /// <summary>Cached thread channel instances.</summary>
    public EntityCache<DiscordThreadChannel> ThreadCache { get; private set; }

    public IAsyncEnumerable<DiscordMessage> GetMessagesAfterAsync(DiscordSnowflake snowflake, ushort limit = 100) 
        => this.Bot.RestClient.GetChannelMessagesAsync(this.Id,)

    public IAsyncEnumerable<DiscordMessage> GetMessagesAroundAsync(DiscordSnowflake snowflake, ushort limit = 100) 
        => throw new System.NotImplementedException();

    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync(ushort limit = 100) 
        => throw new System.NotImplementedException();

    public IAsyncEnumerable<DiscordMessage> GetMessagesBeforeAsync(DiscordSnowflake snowflake, ushort limit = 100) 
        => throw new System.NotImplementedException();

    public Task<DiscordMessage> SendMessageAsync(MessageBuilder builder) 
        => throw new System.NotImplementedException();
}