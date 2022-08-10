namespace Donatello;

using Donatello.Entity;
using Donatello.Extension.Internal;
using Donatello.Rest;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

/// <summary>Abstract implementation of <see cref="IBot"/> with an entity cache.</summary>
public abstract class DiscordBot : IBot
{
    private MemoryCache _userCache, _guildCache, _channelCache, _threadCache;

    public DiscordBot(string token, ILogger logger = null)
    {
        _userCache = new MemoryCache(new MemoryCacheOptions());
        _guildCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());
        _threadCache = new MemoryCache(new MemoryCacheOptions());

        this.Token = token;
        this.Logger = logger ?? NullLogger.Instance;
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, this.Logger);
    }

    /// <summary>Discord API token string.</summary>
    protected internal string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    internal ILogger Logger { get; private init; }

    /// <summary>Discord REST API wrapper.</summary>
    internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Number of guilds in the guild cache.</summary>
    public int GuildCount => _guildCache.Count;

    /// <summary>Connects to the Discord API.</summary>
    public abstract ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    public abstract ValueTask StopAsync();

    /// <summary>Returns <see langword="true"/> if the specified user is in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="user">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool TryGetCachedUser(DiscordSnowflake id, out DiscordUser user)
        => _userCache.TryGetValue(id, out user);

    /// <summary>Returns <see langword="true"/> if the specified guild is in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="guild">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>
    /// </param>
    public bool TryGetCachedGuild(DiscordSnowflake id, out DiscordGuild guild)
        => _guildCache.TryGetValue(id, out guild);

    /// <summary>Returns <see langword="true"/> if the specified channel is in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="channel">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>
    /// </param>
    public bool TryGetCachedChannel<TChannel>(DiscordSnowflake id, out TChannel channel) where TChannel : DiscordChannel
        => _channelCache.TryGetValue(id, out channel);

    /// <summary>Returns <see langword="true"/> if the specified channel is in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="channel">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>
    /// </param>
    public bool TryGetCachedChannel(DiscordSnowflake id, out DiscordChannel channel)
        => _channelCache.TryGetValue(id, out channel);

    /// <summary>Returns <see langword="true"/> if the specified thread is in the cache, <see langword="false"/> otherwise.</summary>
    /// <param name="thread">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the cached instance.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>
    /// </param>
    public bool TryGetCachedThread(DiscordSnowflake id, out DiscordThreadTextChannel thread)
        => _threadCache.TryGetValue(id, out thread);

    /// <inheritdoc/>
    public async ValueTask<DiscordUser> GetUserAsync(DiscordSnowflake userId)
    {
        if (TryGetCachedUser(userId, out DiscordUser user) is false)
        {
            var userJson = await this.RestClient.GetUserAsync(userId);
            user = new DiscordUser(this, userJson);

            UpdateUserCache(user);
        }

        return user;
    }

    /// <inheritdoc/>
    public async ValueTask<DiscordGuild> GetGuildAsync(DiscordSnowflake guildId)
    {
        if (TryGetCachedGuild(guildId, out DiscordGuild guild) is false)
        {
            var guildJson = await this.RestClient.GetGuildAsync(guildId);
            guild = new DiscordGuild(this, guildJson);

            UpdateGuildCache(guild);
        }

        return guild;
    }

    /// <inheritdoc/>
    public async ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : DiscordChannel
    {
        if (TryGetCachedChannel(channelId, out TChannel channel) is false)
        {
            var channelJson = await this.RestClient.GetChannelAsync(channelId);
            channel = (TChannel)channelJson.ToChannelEntity(this);

            UpdateChannelCache(channel);
        }

        return channel;
    }

    /// <inheritdoc/>
    public ValueTask<DiscordChannel> GetChannelAsync(DiscordSnowflake channelId)
        => GetChannelAsync<DiscordChannel>(channelId);

    /// <summary>Adds or updates an entry in the guild cache.</summary>
    protected void UpdateGuildCache(DiscordGuild guild)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .RegisterPostEvictionCallback(LogGuildCacheEviction);

        _guildCache.Set(guild.Id, guild, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in guild cache", guild.Id);

        void LogGuildCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from guild cache", (ulong)key);
    }

    /// <summary>Adds or updates an entry in the channel cache.</summary>
    protected void UpdateChannelCache(DiscordChannel channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(channel.Id, channel, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in channel cache", channel.Id);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from channel cache", (key as DiscordSnowflake).Value);
    }

    protected void UpdateThreadCache(DiscordThreadTextChannel thread)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1))
            .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _threadCache.Set(thread.Id, thread, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in thread cache", thread.Id);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from thread cache", (key as DiscordSnowflake).Value);
    }

    /// <summary>Adds or updates an entry in the user cache.</summary>
    protected void UpdateUserCache(DiscordUser user)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogUserCacheEviction);

        _userCache.Set(user.Id, user, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in user cache", user.Id);

        void LogUserCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from user cache", (ulong)key);
    }

    /// <summary></summary>
    protected DiscordGuild RemoveCachedGuild(DiscordSnowflake id)
    {
        if (TryGetCachedGuild(id, out DiscordGuild cachedGuild))
            _guildCache.Remove(id);

        return cachedGuild;
    }

    /// <summary></summary>
    protected DiscordChannel RemoveCachedChannel(DiscordSnowflake id)
    {
        if (TryGetCachedChannel(id, out DiscordChannel cachedChannel))
            _channelCache.Remove(id);

        return cachedChannel;
    }

    /// <summary></summary>
    protected DiscordThreadTextChannel RemoveCachedThread(DiscordSnowflake id)
    {
        if (TryGetCachedThread(id, out DiscordThreadTextChannel thread))
            _threadCache.Remove(id);

        return thread;
    }

    /// <summary></summary>
    protected DiscordUser RemoveCachedUser(DiscordSnowflake id)
    {
        if (TryGetCachedUser(id, out DiscordUser cachedUser))
            _userCache.Remove(id);

        return cachedUser;
    }
}
