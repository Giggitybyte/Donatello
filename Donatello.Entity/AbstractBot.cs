namespace Donatello.Core;

using Donatello.Core.Entity;
using Donatello.Core.Rest;
using Microsoft.Extensions.Logging;

/// <summary></summary>
public abstract class AbstractBot
{
    internal AbstractBot(string token, ILogger logger = null)
    {
        this.RestClient = new DiscordHttpClient(token, logger);
    }

    /// <summary>REST API wrapper instance.</summary>
    internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Connects to the Discord API.</summary>
    public abstract ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources.</summary>
    public abstract ValueTask StopAsync();

    /// <summary></summary>
    public abstract ValueTask<DiscordUser> GetUserAsync(ulong userId);

    /// <summary></summary>
    public abstract ValueTask<DiscordGuild> GetGuildAsync(ulong guildId);

    /// <summary></summary>
    public abstract ValueTask<TChannel> GetChannelAsync<TChannel>(ulong channelId) where TChannel : DiscordChannel;
}
