namespace Donatello;

using Donatello.Entity;
using System.Threading.Tasks;

public interface IBot
{
    /// <summary>Connects to the Discord API.</summary>
    ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    ValueTask StopAsync();

    /// <summary>Fetches a user object using a snowflake ID.</summary>
    ValueTask<DiscordUser> GetUserAsync(DiscordSnowflake userId);

    /// <summary>Fetches a guild object using an ID.</summary>
    ValueTask<DiscordGuild> GetGuildAsync(DiscordSnowflake guildId);

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : DiscordChannel;

    /// <summary>Fetches a channel using a snowflake ID.</summary>
    ValueTask<DiscordChannel> GetChannelAsync(DiscordSnowflake channelId);
}