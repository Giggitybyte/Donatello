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
using System.Threading.Tasks;

/// <summary></summary>
public abstract class DiscordApiBot
{
    private MemoryCache _guildCache, _channelCache, _userCache, _presenceCache;

    public DiscordApiBot(string token, ILogger logger = null)
    {
        _guildCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());
        _userCache = new MemoryCache(new MemoryCacheOptions());
        _presenceCache = new MemoryCache(new MemoryCacheOptions());

        this.Token = token;
        this.Logger = logger ?? NullLogger.Instance;
        this.RestClient = new DiscordHttpClient(token, this.Logger);
    }

    /// <summary>Bot user token string.</summary>
    protected string Token { get; private init; }

    /// <summary></summary>
    internal ILogger Logger { get; private init; }

    /// <summary>REST API wrapper instance.</summary>
    internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Connects to the Discord API.</summary>
    public abstract ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources.</summary>
    public abstract ValueTask StopAsync();

    /// <summary>Fetches a guild object using an ID.</summary>
    public async ValueTask<DiscordGuild> GetGuildAsync(ulong guildId)
    {
        if (_guildCache.TryGetValue<DiscordGuild>(guildId, out var cachedGuild))
            return cachedGuild;
        else
        {
            var json = await this.RestClient.GetGuildAsync(guildId);
            var guild = new DiscordGuild(this, json);

            UpdateGuildCache(guild);
            this.Logger.LogTrace("Added entry {Id} to the guild cache", guildId);

            return guild;
        }
    }

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public async ValueTask<TChannel> GetChannelAsync<TChannel>(ulong channelId) where TChannel : DiscordChannel
    {
        if (_channelCache.TryGetValue(channelId, out TChannel cachedChannel))
            return cachedChannel;
        else
        {
            var json = await this.RestClient.GetChannelAsync(channelId);
            var channel = json.ToChannelEntity(this);

            UpdateChannelCache(channel);
            this.Logger.LogTrace("Added entry {Id} to the channel cache", channelId);

            return channel as TChannel;
        }
    }

    /// <summary>Fetches a user object using an ID.</summary>
    public async ValueTask<DiscordUser> GetUserAsync(ulong userId)
    {
        if (_userCache.TryGetValue<DiscordUser>(userId, out var cachedUser))
            return cachedUser;
        else
        {
            var json = await this.RestClient.GetUserAsync(userId);
            var user = new DiscordUser(this, json);

            UpdateUserCache(user);
            this.Logger.LogTrace("Added entry {Id} to the user cache", userId);


            return user;
        }
    }

    /// <summary>Adds or updates an entry in the guild cache.</summary>
    protected void UpdateGuildCache(DiscordGuild guild)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1))
            .RegisterPostEvictionCallback(LogGuildCacheEviction);

        _guildCache.Set(guild.Id, guild, entryConfig);

        void LogGuildCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the guild cache ({Reason})", (ulong)key, reason);
    }

    /// <summary>Adds or updates an entry in the channel cache.</summary>
    protected void UpdateChannelCache(DiscordChannel channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(channel.Id, channel, entryConfig);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the channel cache ({Reason})", (ulong)key, reason);
    }

    /// <summary>Adds or updates an entry in the user cache.</summary>
    protected void UpdateUserCache(DiscordUser user)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(LogUserCacheEviction);

        _userCache.Set(user.Id, user, entryConfig);

        void LogUserCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed entry {Id} from the user cache ({Reason})", (ulong)key, reason);
    }
}
