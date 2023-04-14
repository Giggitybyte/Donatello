namespace Donatello.Common;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cache;
using Rest;
using Donatello.Rest.Extension.Endpoint;
using Entity.Channel;
using Entity.Guild;
using Entity.User;
using Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Common methods and properties for a Discord bot.</summary>
public abstract class Bot
{
    protected Bot(string token, ILoggerFactory loggerFactory)
    {
        this.Token = token;
        this.LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this.Logger = this.LoggerFactory.CreateLogger("Discord Bot");
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, this.LoggerFactory.CreateLogger("Discord REST"));
    }

    /// <summary>Logger factory instance.</summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>Discord API token string.</summary>
    protected string Token { get; }

    /// <summary>Logging instance.</summary>
    protected ILogger Logger { get; }

    /// <summary></summary>
    protected internal MemoryCache UserCache { get; } = new();

    /// <summary></summary>
    protected internal MemoryCache GuildCache { get; } = new();

    /// <summary></summary>
    protected internal MemoryCache ChannelCache { get; } = new();

    /// <summary></summary>
    protected internal MemoryCache MessageCache { get; } = new();

    /// <summary></summary>
    protected internal MemoryCache VoiceStateCache { get; } = new();

    /// <summary></summary>
    protected internal AggregateCache GuildMemberCache { get; } = new();

    /// <summary></summary>
    protected internal AggregateCache GuildRoleCache { get; } = new();

    /// <summary></summary>
    protected internal AggregateCache GuildThreadCache { get; } = new();
    
    /// <summary></summary>
    protected internal AggregateCache ThreadMemberCache { get; } = new();

    /// <summary>Discord REST API wrapper.</summary>
    public DiscordHttpClient RestClient { get; }
    
    /// <summary>Whether this instance is connected to the Discord API.</summary>
    public abstract bool IsConnected { get; }

    /// <summary>Connects to the Discord API.</summary>
    public abstract Task StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    public abstract Task StopAsync();

    /// <summary>Connects to the Discord API and waits until <paramref name="cancellationToken"/> is cancelled.</summary>
    /// <remarks>When <paramref name="cancellationToken"/> is cancelled, this instance will disconnect from the API and the <see cref="Task"/> returned by this method will complete.</remarks>
    public virtual async Task RunAsync(CancellationToken cancellationToken)
    {
        await this.StartAsync();
        await Task.Delay(-1, cancellationToken).ContinueWith(delayTask => this.StopAsync()).Unwrap();
    }

    /// <summary>Requests an up-to-date user object from Discord.</summary>
    public virtual async Task<User> FetchUserAsync(Snowflake userId)
    {
        var userJson = await this.RestClient.GetUserAsync(userId);
        this.UserCache.AddOrUpdate(userId, userJson);

        return new User(userJson, this);
    }

    /// <summary>Attempts to get a user from the cache; if <paramref name="userId"/> is not present in cache, an up-to-date user will be fetched from Discord.</summary>
    public virtual async ValueTask<User> GetUserAsync(Snowflake userId)
    {
        if (this.UserCache.TryGetEntry(userId, out JsonElement userJson))
            return new User(userJson, this);
        else
            return await this.FetchUserAsync(userId);
    }

    /// <summary>Requests an up-to-date guild object from Discord.</summary>
    public virtual async Task<Guild> FetchGuildAsync(Snowflake guildId)
    {
        var guildJson = await this.RestClient.GetGuildAsync(guildId);
        this.GuildCache.AddOrUpdate(guildId, guildJson);

        return new Guild(guildJson, this);
    }

    /// <summary>Attempts to get a guild from the cache; if <paramref name="guildId"/> is not present in cache, an up-to-date guild will be fetched from Discord.</summary>
    public virtual async ValueTask<Guild> GetGuildAsync(Snowflake guildId)
    {
        if (this.GuildCache.TryGetEntry(guildId, out JsonElement guildJson))
            return new Guild(guildJson, this);
        else
            return await this.FetchGuildAsync(guildId);
    }

    /// <summary></summary>
    public virtual async Task<TChannel> FetchChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IChannel
    {
        var channelJson = await this.RestClient.GetChannelAsync(channelId);
        this.ChannelCache.AddOrUpdate(channelId, channelJson);
        return channelJson.AsChannel<TChannel>(this);
    }

    /// <summary></summary>
    public virtual Task<IChannel> FetchChannelAsync(Snowflake channelId)
        => this.FetchChannelAsync<IChannel>(channelId);
    
    /// <summary></summary>
    public virtual async IAsyncEnumerable<TChannel> FetchChannelsAsync<TChannel>(IEnumerable<Snowflake> channelIds) where TChannel : class, IChannel
    {
        foreach (var channelId in channelIds)
            yield return await this.FetchChannelAsync<TChannel>(channelId);
    }

    /// <summary></summary>
    public virtual IAsyncEnumerable<IChannel> FetchChannelsAsync(IEnumerable<Snowflake> channelIds)
        => this.FetchChannelsAsync<IChannel>(channelIds);

    /// <inheritdoc cref="GetChannelAsync(Snowflake)"/>
    public virtual async ValueTask<TChannel> GetChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IChannel
    {
        if (this.ChannelCache.TryGetEntry(channelId, out JsonElement channelJson))
            return channelJson.AsChannel<TChannel>(this);
        else
            return await this.FetchChannelAsync<TChannel>(channelId);
    }

    /// <summary>Attempts to get a channel from the cache; if <paramref name="channelId"/> is not present in cache, an up-to-date channel will be fetched from Discord.</summary>
    public virtual ValueTask<IChannel> GetChannelAsync(Snowflake channelId)
        => this.GetChannelAsync<IChannel>(channelId);

    /// <summary></summary>
    public virtual async IAsyncEnumerable<TChannel> GetChannelsAsync<TChannel>(IEnumerable<Snowflake> channelIds) where TChannel : class, IChannel
    {
        foreach (var channelId in channelIds)
            yield return await this.GetChannelAsync<TChannel>(channelId);
    }

    /// <summary></summary>
    public virtual IAsyncEnumerable<IChannel> GetChannelsAsync(IEnumerable<Snowflake> channelIds)
        => this.GetChannelsAsync<IChannel>(channelIds);
}