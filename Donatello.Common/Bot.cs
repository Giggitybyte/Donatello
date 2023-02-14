namespace Donatello.Common;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Donatello.Rest;
using Donatello.Rest.Extension.Endpoint;
using Entity.Channel;
using Entity.Guild;
using Entity.Guild.Channel;
using Entity.User;
using Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Common methods and properties for a Discord bot.</summary>
public abstract class Bot
{
    private JsonCache _userCache, _guildCache, _channelCache, _threadCache, _messageCache;

    protected Bot(string token, ILoggerFactory loggerFactory, cache)
    {
        this.LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this.Logger = this.LoggerFactory.CreateLogger("Discord Bot");
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, this.LoggerFactory.CreateLogger("Discord REST"));
        this.Token = token;

        _userCache = new JsonCache();
    }

    /// <summary>Logger factory instance.</summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>Discord API token string.</summary>
    protected internal string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    protected internal ILogger Logger { get; private init; }

    /// <summary>Discord REST API wrapper.</summary>
    public DiscordHttpClient RestClient { get; private init; }

    /// <summary>Whether this instance is connected to the Discord API.</summary>
    public abstract bool IsConnected { get; }

    /// <summary>Connects to the Discord API.</summary>
    public abstract Task StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    public abstract Task StopAsync();

    /// <summary>Connects to the Discord API and waits until <paramref name="cancellationToken"/> is cancelled.</summary>
    /// <remarks>When <paramref name="cancellationToken"/> is cancelled, this instance will be disconnected from the API and the <see cref="Task"/> returned by this method will complete.</remarks>
    public virtual async Task RunAsync(CancellationToken cancellationToken)
    {
        await this.StartAsync();
        await Task.Delay(-1, cancellationToken).ContinueWith(delayTask => this.StopAsync()).Unwrap();
    }

    /// <summary>Requests an up-to-date user object from Discord.</summary>
    public virtual async Task<User> FetchUserAsync(Snowflake userId)
    {
        var json = await this.RestClient.GetUserAsync(userId);
        _userCache.Add(json);
        
        return new User(json);
    }

    /// <summary>Attempts to get a user from the cache; if <paramref name="userId"/> is not present in cache, an up-to-date user will be fetched from Discord.</summary>
    public virtual async ValueTask<User> GetUserAsync(Snowflake userId)
    {
        if (_userCache.TryGet(userId, out JsonElement json))
            return new User(json);
        else
            return await this.FetchUserAsync(userId);
    }

    /// <summary>Requests an up-to-date guild object from Discord.</summary>
    public virtual async Task<Guild> FetchGuildAsync(Snowflake guildId)
    {
        var json = await this.RestClient.GetGuildAsync(guildId);
        _guildCache.Add(json);

        return new Guild(json);
    }

    public virtual async ValueTask<Guild> GetGuildAsync(Snowflake guildId)
    {
        if (_guildCache.TryGet(guildId, out JsonElement json))
            return new Guild(json);
        else
            return await this.FetchGuildAsync(guildId);
    }

    /// <summary></summary>
    public virtual async Task<TChannel> FetchChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IChannel
    {
        var channelJson = await this.RestClient.GetChannelAsync(channelId);
        var channel = Channel.Create<TChannel>(this, channelJson);

        return channel;
    }

    /// <summary></summary>
    public virtual Task<IChannel> FetchChannelAsync(Snowflake channelId)
        => this.FetchChannelAsync<IChannel>(channelId);

    /// <inheritdoc cref="GetChannelAsync(Snowflake)"/>
    public virtual async ValueTask<TChannel> GetChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IChannel
    {
        foreach (var guild in this.GuildCache)
        {
            if (guild.ChannelCache.TryGet(channelId, out IGuildChannel cachedChannel))
                return cachedChannel as TChannel;
        }

        return await this.FetchChannelAsync<TChannel>(channelId);
    }
}
