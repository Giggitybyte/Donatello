namespace Donatello;

using Donatello.Entity;
using Donatello.Extension.Internal;
using Donatello.Rest;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;

public abstract class DiscordBot
{
    public DiscordBot(string token, ILogger logger = null)
    {
        this.Token = token;
        this.Logger = logger ?? NullLogger.Instance;
        this.RestClient = new DiscordHttpClient(TokenType.Bot, token, this.Logger);
        this.UserCache = new ObjectCache<DiscordUser>();
        this.GuildCache = new ObjectCache<DiscordGuild>();
        this.ChannelCache = new ObjectCache<DiscordChannel>();
    }

    /// <summary>Discord API token string.</summary>
    protected internal string Token { get; private init; }

    /// <summary>Logging instance.</summary>
    protected internal ILogger Logger { get; private init; }

    /// <summary>Discord REST API wrapper.</summary>
    protected internal DiscordHttpClient RestClient { get; private init; }

    /// <summary>Cached user instances.</summary>
    public ObjectCache<DiscordUser> UserCache { get; private init; }

    /// <summary>Cached guild instances.</summary>
    public ObjectCache<DiscordGuild> GuildCache { get; private init; }

    /// <summary>Cached channel instances.</summary>
    public ObjectCache<DiscordChannel> ChannelCache { get; private init; }

    /// <summary>Connects to the Discord API.</summary>
    public abstract ValueTask StartAsync();

    /// <summary>Disconnects from the Discord API and releases any resources in use.</summary>
    public abstract ValueTask StopAsync();

    /// <summary>Fetches a user object using a snowflake ID.</summary>
    public virtual async ValueTask<DiscordUser> GetUserAsync(DiscordSnowflake userId)
    {
        if (this.UserCache.Contains(userId, out DiscordUser user) is false)
        {
            var userJson = await this.RestClient.GetUserAsync(userId);
            user = new DiscordUser(this, userJson);

            this.UserCache.Add(userId, user);
        }

        return user;
    }

    /// <summary>Fetches a guild object using an ID.</summary>
    public virtual async ValueTask<DiscordGuild> GetGuildAsync(DiscordSnowflake guildId)
    {
        if (this.GuildCache.Contains(guildId, out DiscordGuild guild) is false)
        {
            var guildJson = await this.RestClient.GetGuildAsync(guildId);
            guild = new DiscordGuild(this, guildJson);

            this.GuildCache.Add(guildId, guild);
        }

        return guild;
    }

    /// <summary>Fetches a channel object using an ID.</summary>
    public virtual ValueTask<DiscordChannel> GetChannelAsync(DiscordSnowflake channelId)
        => this.GetChannelAsync<DiscordChannel>(channelId);

    /// <summary>Fetches a channel using an ID and returns it as a <typeparamref name="TChannel"/> object.</summary>
    public virtual async ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : DiscordChannel
    {
        if (this.ChannelCache.Contains(channelId, out DiscordChannel channel) is false)
        {
            var channelJson = await this.RestClient.GetChannelAsync(channelId);
            channel = channelJson.ToChannelEntity(this);

            this.ChannelCache.Add(channelId, channel);
        }

        return (TChannel)channel;
    }
}
