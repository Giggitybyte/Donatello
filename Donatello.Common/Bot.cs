namespace Donatello;

using Entity;
using Extension.Internal;
using Rest;
using Donatello.Rest.Extension.Endpoint;
using Type;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Common methods and properties for a Discord bot.</summary>
public abstract class Bot
{
    private ILoggerFactory _loggerFactory;

    protected Bot(string token, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        
        this.Token = token;
        this.Logger = _loggerFactory.CreateLogger("Discord Bot");
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, _loggerFactory.CreateLogger("Discord REST Client"));
        this.GuildCache = new EntityCache<Guild>();
        this.UserCache = new EntityCache<User>();

    }

    /// <summary>Logger factory instance.</summary>
    protected ILoggerFactory LoggerFactory => _loggerFactory;

    /// <summary>Discord API token string.</summary>
    protected internal string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    protected internal ILogger Logger { get; private init; }

    /// <summary>Discord REST API wrapper.</summary>
    protected internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Cached user instances.</summary>
    public EntityCache<User> UserCache { get; private init; }

    /// <summary>Cached guild instances.</summary>
    public EntityCache<Guild> GuildCache { get; private init; }

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
        var userJson = await this.RestClient.GetUserAsync(userId);
        var user = new User(this, userJson);
        this.UserCache.Add(userId, user);

        return user;
    }

    /// <summary>Attempts to get a user from the cache; if <paramref name="userId"/> is not present in cache, an up-to-date user will be fetched from Discord.</summary>
    public virtual async ValueTask<User> GetUserAsync(Snowflake userId)
    {
        if (this.UserCache.TryGet(userId, out User user) is false)
            user = await this.FetchUserAsync(userId);

        return user;
    }

    /// <summary>Requests an up-to-date guild object from Discord.</summary>
    public virtual async Task<Guild> FetchGuildAsync(Snowflake guildId)
    {
        var guildJson = await this.RestClient.GetGuildAsync(guildId);
        var guild = new Guild(this, guildJson);
        this.GuildCache.Add(guildId, guild);

        return guild;
    }

    /// <summary>Attempts to get a guild from the cache; if <paramref name="guildId"/> is not present in cache, an up-to-date guild will be fetched from Discord.</summary>
    public virtual async ValueTask<Guild> GetGuildAsync(Snowflake guildId)
    {
        if (this.GuildCache.TryGet(guildId, out Guild guild) is false)
            guild = await this.FetchGuildAsync(guildId);

        return guild;
    }

    /// <summary></summary>
    public virtual async Task<TChannel> FetchChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IChannel
    {
        var channelJson = await this.RestClient.GetChannelAsync(channelId);
        var channel = Channel.Create<TChannel>(this, channelJson);

        if (channelJson.TryGetProperty("guild_id", out JsonElement prop))
        {
            if (channel is IGuildChannel guildChannel)
            {
                var guild = await this.GetGuildAsync(prop.ToSnowflake());
                guild.ChannelCache.Add(channelId, guildChannel);
            }
            else
                throw new InvalidCastException($"{typeof(TChannel).Name} is not a valid type parameter for guild channel objects.");
        }

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

    /// <summary>Attempts to get a channel from the cache; if <paramref name="channelId"/> is not present in cache, an up-to-date channel will be fetched from Discord.</summary>
    public virtual ValueTask<Channel> GetChannelAsync(Snowflake channelId)
        => this.GetChannelAsync<Channel>(channelId);
}
