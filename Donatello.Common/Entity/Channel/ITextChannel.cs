namespace Donatello.Entity;

using Donatello.Entity.Builder;
using System.Threading.Tasks;

public interface ITextChannel : IChannel
{
    public Task<EntityCollection<DiscordMessage>> GetMessagesAsync();

    public Task<EntityCollection<DiscordMessage>> GetMessagesAroundAsync(DiscordSnowflake snowflake);

    public Task<EntityCollection<DiscordMessage>> GetMessagesBeforeAsync(DiscordSnowflake snowflake);

    public Task<EntityCollection<DiscordMessage>> GetMessagesAfterAsync(DiscordSnowflake snowflake);

    public Task<DiscordMessage> SendMessageAsync(MessageBuilder builder);

}

