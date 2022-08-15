namespace Donatello;

using Donatello.Entity;
using Donatello.Extension.Internal;
using Donatello.Rest;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

/// <summary>Abstract implementation of <see cref="IBot"/>.</summary>
public abstract class DiscordBot : IBot
{
    public DiscordBot(string token, ILogger logger = null)
    {
        this.Token = token;
        this.Logger = logger ?? NullLogger.Instance;
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, this.Logger);
        this.UserCache = new EntityCache<DiscordUser>(TimeSpan.FromMinutes(30), TimeSpan.FromHours(1));
        this.GuildCache = new EntityCache<DiscordGuild>(TimeSpan.FromMinutes(30), TimeSpan.FromHours(2));
        this.ChannelCache = new EntityCache<DiscordChannel>(TimeSpan.FromMinutes(30), TimeSpan.FromHours(1));
    }

    /// <summary>Discord API token string.</summary>
    protected internal string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    protected internal ILogger Logger { get; private init; }

    /// <summary>Discord REST API wrapper.</summary>
    protected internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Cached user instances.</summary>
    public EntityCache<DiscordUser> UserCache { get; private init; }

    /// <summary>Cached guild instances.</summary>
    public EntityCache<DiscordGuild> GuildCache { get; private init; }

    /// <summary>Cached channel instances.</summary>
    public EntityCache<DiscordChannel> ChannelCache { get; private init; }

    /// <inheritdoc cref="IBot.StartAsync"/>
    public abstract ValueTask StartAsync();

    /// <inheritdoc cref="IBot.StopAsync"/>
    public abstract ValueTask StopAsync();

    /// <inheritdoc cref="IBot.GetUserAsync(DiscordSnowflake)"/>
    public virtual async ValueTask<DiscordUser> GetUserAsync(DiscordSnowflake userId)
    {
        if (this.UserCache.TryGetEntity(userId, out DiscordUser user) is false)
        {
            var userJson = await this.RestClient.GetUserAsync(userId);
            user = new DiscordUser(this, userJson);

            this.UserCache.Add(user);
        }

        return user;
    }

    /// <inheritdoc cref="IBot.GetGuildAsync(DiscordSnowflake)"/>
    public virtual async ValueTask<DiscordGuild> GetGuildAsync(DiscordSnowflake guildId)
    {
        var guildJson = await this.RestClient.GetGuildAsync(guildId);
        return new DiscordGuild(this, guildJson);
    }

    /// <inheritdoc cref="IBot.GetChannelAsync(DiscordSnowflake)"/>
    public virtual ValueTask<DiscordChannel> GetChannelAsync(DiscordSnowflake channelId)
        => GetChannelAsync<DiscordChannel>(channelId);

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public virtual async ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : DiscordChannel
    {
        var channelJson = await this.RestClient.GetChannelAsync(channelId);
        return (TChannel)channelJson.ToChannelEntity(this);
    }
}
