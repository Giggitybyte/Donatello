namespace Donatello;

using Donatello.Entity;
using Donatello.Extension.Internal;
using Donatello.Rest;
using Donatello.Rest.Channel;
using Donatello.Rest.Guild;
using Donatello.Rest.User;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public abstract class DiscordApiBot
{
    private MemoryCache _userCache, _guildCache, _channelCache;

    public DiscordApiBot(string token, ILogger logger = null)
    {
        _userCache = new MemoryCache(new MemoryCacheOptions());
        _guildCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());

        this.Token = token;
        this.Logger = logger ?? NullLogger.Instance;
        this.RestClient = new DiscordHttpClient(token, this.Logger);
    }

    /// <summary>Discord API token string.</summary>
    protected string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    internal ILogger Logger { get; private init; }

    /// <summary>REST API wrapper instance.</summary>
    internal DiscordHttpClient RestClient { get; private init; }

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

    /// <summary>Fetches a user object using an ID.</summary>
    public async ValueTask<DiscordUser> GetUserAsync(DiscordSnowflake userId)
    {
        if (TryGetCachedUser(userId, out DiscordUser user) is false)
        {
            var userJson = await this.RestClient.GetUserAsync(userId);
            user = new DiscordUser(this, userJson);

            UpdateUserCache(userId, user);
        }

        return user;
    }

    /// <summary>Fetches a guild object using an ID.</summary>
    public async ValueTask<DiscordGuild> GetGuildAsync(DiscordSnowflake guildId)
    {
        if (TryGetCachedGuild(guildId, out DiscordGuild guild) is false)
        {
            var guildJson = await this.RestClient.GetGuildAsync(guildId);
            guild = new DiscordGuild(this, guildJson);

            UpdateGuildCache(guildId, guild);
        }

        return guild;
    }

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public async ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : DiscordChannel
    {
        if (TryGetCachedChannel(channelId, out TChannel channel) is false)
        {
            var channelJson = await this.RestClient.GetChannelAsync(channelId);
            channel = (TChannel)channelJson.ToChannelEntity(this);

            UpdateChannelCache(channelId, channel);
        }

        return channel;
    }

    /// <summary>Adds or updates an entry in the guild cache.</summary>
    protected void UpdateGuildCache(DiscordSnowflake id, DiscordGuild guild)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .RegisterPostEvictionCallback(LogGuildCacheEviction);

        _guildCache.Set(id, guild, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in guild cache", id);

        void LogGuildCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from guild cache", (ulong)key);
    }

    /// <summary>Adds or updates an entry in the channel cache.</summary>
    protected void UpdateChannelCache(DiscordSnowflake id, DiscordChannel channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(id, channel, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in channel cache", id);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from channel cache", (ulong)key);
    }

    /// <summary>Adds or updates an entry in the user cache.</summary>
    protected void UpdateUserCache(DiscordSnowflake id, DiscordUser user)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(20))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogUserCacheEviction);

        _userCache.Set(id, user, entryConfig);
        this.Logger.LogTrace("Updated entry {Id} in user cache", id);

        void LogUserCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed stale entry {Id} from user cache", (ulong)key);
    }
}
