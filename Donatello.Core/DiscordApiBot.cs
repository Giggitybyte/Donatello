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
    private MemoryCache _guildCache, _channelCache, _userCache;

    public DiscordApiBot(string token, ILogger logger = null)
    {
        _guildCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());
        _userCache = new MemoryCache(new MemoryCacheOptions());

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

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    public abstract ValueTask StopAsync();

    /// <summary>Fetches a guild object using an ID.</summary>
    public async ValueTask<DiscordGuild> GetGuildAsync(ulong guildId)
        => new DiscordGuild(this, await GetGuildJsonAsync(guildId));

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public async ValueTask<TChannel> GetChannelAsync<TChannel>(ulong channelId) where TChannel : DiscordChannel
    {
        var json = await GetChannelJsonAsync(channelId);
        return (TChannel)json.ToChannelEntity(this);
    }

    /// <summary>Fetches a user object using an ID.</summary>
    public async ValueTask<DiscordUser> GetUserAsync(ulong userId)
        => new DiscordUser(this, await GetUserJsonAsync(userId));

    /// <summary>Returns a JSON guild object for provided snowflake ID.</summary>
    internal async ValueTask<JsonElement> GetGuildJsonAsync(ulong guildId)
    {
        JsonElement guild;

        if (_guildCache.TryGetValue(guildId, out guild) is false)
        {
            guild = await this.RestClient.GetGuildAsync(guildId);
            UpdateGuildCache(guildId, guild);
        }

        return guild;
    }

    /// <summary>Returns a JSON channel object for provided snowflake ID.</summary>
    internal async ValueTask<JsonElement> GetChannelJsonAsync(ulong channelId)
    {
        JsonElement channel;

        if (_channelCache.TryGetValue(channelId, out channel) is false)
        {
            channel = await this.RestClient.GetChannelAsync(channelId);
            UpdateChannelCache(channelId, channel);
        }

        return channel;
    }

    /// <summary>Returns a JSON user object for provided snowflake ID.</summary>
    internal async ValueTask<JsonElement> GetUserJsonAsync(ulong userId)
    {
        JsonElement user;

        if (_userCache.TryGetValue(userId, out user) is false)
        {
            user = await this.RestClient.GetUserAsync(userId);
            UpdateUserCache(userId, user);
        }

        return user;
    }

    /// <summary>Adds or updates an entry in the guild cache.</summary>
    protected void UpdateGuildCache(ulong id, JsonElement guild)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .RegisterPostEvictionCallback(LogGuildCacheEviction);

        _guildCache.Set(id, guild, entryConfig);
        this.Logger.LogTrace("Updated {Id} in guild cache", id);

        void LogGuildCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed {Id} from guild cache", (ulong)key);
    }

    /// <summary>Adds or updates an entry in the channel cache.</summary>
    protected void UpdateChannelCache(ulong id, JsonElement channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(id, channel, entryConfig);
        this.Logger.LogTrace("Updated {Id} in channel cache", id);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed {Id} from channel cache", (ulong)key);
    }

    /// <summary>Adds or updates an entry in the user cache.</summary>
    protected void UpdateUserCache(ulong id, JsonElement user)
    {
        var entryConfig = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .RegisterPostEvictionCallback(LogUserCacheEviction);

        _userCache.Set(id, user, entryConfig);
        this.Logger.LogTrace("Updated {Id} in user cache", id);

        void LogUserCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Logger.LogTrace("Removed {Id} from user cache", (ulong)key);
    }
}
