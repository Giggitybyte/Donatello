namespace Donatello;

using Donatello.Entity;
using Donatello.Rest;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

/// <summary></summary>
public abstract class DiscordApiBot
{
    public DiscordApiBot(string token, ILogger logger = null)
    {
        this.RestClient = new DiscordHttpClient(token, logger);
    }

    /// <summary>REST API wrapper instance.</summary>
    internal DiscordHttpClient RestClient { get; private init; }

    /// <summary></summary>
    internal abstract ValueTask<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong messageId);

    /// <summary>Connects to the Discord API.</summary>
    public abstract ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources.</summary>
    public abstract ValueTask StopAsync();

    /// <summary>Retrieves a user by ID.</summary>
    public abstract ValueTask<DiscordUser> GetUserAsync(ulong userId);

    /// <summary>Retrieves a guild by ID.</summary>
    public abstract ValueTask<DiscordGuild> GetGuildAsync(ulong guildId);

    /// <summary>Retrieves a channel by ID.</summary>
    public abstract ValueTask<TChannel> GetChannelAsync<TChannel>(ulong channelId) where TChannel : DiscordChannel;
}
