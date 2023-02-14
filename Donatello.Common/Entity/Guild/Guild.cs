namespace Donatello.Common.Entity.Guild;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Enum;
using User;
using Voice;

/// <summary>A collection of channels and members.</summary>
public partial class Guild : Entity
{
    internal Guild(JsonElement json)
        : base(json)
    {
        this.MemberCache = new JsonCache(json => json.GetProperty("user").GetProperty("id").ToSnowflake());
        this.ChannelCache = new EntityCache<IGuildChannel>();
        this.RoleCache = new EntityCache<Role>();
    }

    /// <summary>Cached guild member JSON objects.</summary>
    internal JsonCache MemberCache { get; private init; }

    /// <summary>Cached role instances.</summary>
    public EntityCache<Role> RoleCache { get; private set; }

    /// <summary>Cached channel instances associated with this guild.</summary>
    public EntityCache<IGuildChannel> ChannelCache { get; private set; }

    /// <summary>Cached voice state instances.</summary>
    public ObjectCache<DiscordVoiceState> VoiceStateCache { get; private set; }

    /// <summary>Name of this guild.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Permissions in the guild, excluding channel overwrites.</summary>
    public GuildPermission Permissions => throw new NotImplementedException();

    /// <summary>Returns whether this guild has a specific feature enabled.</summary>
    public bool HasFeature(string feature)
        => this.Json.GetProperty("features").EnumerateArray().Any(guildFeature => guildFeature.GetString() == feature.ToUpper());

    /// <summary>Returns <see langword="true"/> if the guild has an icon image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="iconUrl">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the icon URL.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasIcon(out string iconUrl)
    {
        if (this.Json.TryGetProperty("icon", out var prop) && prop.ValueKind is not JsonValueKind.Null)
        {
            var iconHash = prop.GetString();
            var extension = iconHash.StartsWith("a_") ? "gif" : "png";
            iconUrl = $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";
        }
        else
            iconUrl = string.Empty;

        return iconUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an invite splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">
    /// When the method returns:<br/>
    /// - <see langword="true"/> this parameter will conatain the invite splash URL.<br/>
    /// - <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasInviteSplash(out string splashUrl)
    {
        splashUrl = (this.Json.TryGetProperty("splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            ? $"https://cdn.discordapp.com/splashes/{this.Id}/{property.GetString()}.png"
            : string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an discovery splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">
    /// When the method returns:<br/>
    /// - <see langword="true"/> this parameter will conatain the discovery splash URL.<br/>
    /// - <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasDiscoverySplash(out string splashUrl)
    {
        splashUrl = (this.Json.TryGetProperty("discovery_splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            ? $"https://cdn.discordapp.com/discovery-splashes/{this.Id}/{property.GetString()}.png"
            : string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an AFK voice channel set, <see langword="false"/> otherwise.</summary>
    /// <param name="voiceChannel"> If the method returns <see langword="true"/> this parameter will conatain a <see cref="GuildVoiceChannel"/> instance, otherwise <see langword="null"/>.</param>
    public bool HasAfkChannel(out GuildVoiceChannel voiceChannel)
    {
        // figure out a better construct...
        voiceChannel = null; 

        return voiceChannel != null;
    }

    /// <summary></summary>
    public ValueTask<User> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").ToSnowflake());

    /// <summary></summary>
    public async IAsyncEnumerable<Role> FetchRolesAsync()
    {
        var roles = this.Bot.RestClient.GetGuildRolesAsync(this.Id)
            .Select(json => new Role(this.Bot, json));

        await foreach (var role in roles)
        {
            this.RoleCache.Add(role.Id, role);
            yield return role;
        }
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<Role>> GetRolesAsync()
    {
        this.RoleCache.Clear();

        var roles = new List<Role>();
        await foreach (var role in this.FetchRolesAsync())
            roles.Add(role);

        return roles.AsReadOnly();
    }

    /// <summary></summary>
    public async ValueTask<Role> GetRoleAsync(Snowflake roleId)
    {
        if (this.RoleCache.TryGet(roleId, out Role role) is false)
            role = await this.FetchRolesAsync().SingleOrDefaultAsync(role => role.Id == roleId);

        return role;
    }


    /// <summary></summary>
    public async IAsyncEnumerable<GuildMember> FetchMembersAsync()
    {
        this.MemberCache.Clear();

        await foreach (var memberJson in this.Bot.RestClient.GetGuildMembersAsync(this.Id))
        {
            var userId = memberJson.GetProperty("id").ToSnowflake();
            this.MemberCache.Add(userId, memberJson);

            var user = await this.Bot.GetUserAsync(userId);
            yield return new GuildMember(this.Bot, this.Id, user, memberJson);
        }
    }

    /// <summary></summary>
    public async ValueTask<GuildMember> GetMemberAsync(Snowflake userId)
    {
        if (this.MemberCache.TryGet(userId, out JsonElement memberJson) is false)
        {
            memberJson = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            this.MemberCache.Add(userId, memberJson);
        }

        var user = await this.Bot.GetUserAsync(userId);
        return new GuildMember(this.Bot, this.Id, user, memberJson);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<GuildTextChannel> FetchChannelsAsync()
    {
        var channels = this.Bot.RestClient.GetGuildChannelsAsync(this.Id)
            .Select(channelJson => Common.Entity.Channel.Channel.Create<GuildTextChannel>(this.Bot, channelJson));

        await foreach (var channel in channels)
            yield return channel;
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<GuildTextChannel>> GetChannelsAsync()
    {
        this.ChannelCache.Clear();

        var channels = new List<GuildTextChannel>();
        await foreach (var channel in this.FetchChannelsAsync())
        {
            channels.Add(channel);
            this.ChannelCache.Add(channel);
        }

        return channels.AsReadOnly();
    }

    /// <inheritdoc cref="Bot.GetChannelAsync(Snowflake)"/>
    public ValueTask<TChannel> GetChannelAsync<TChannel>(Snowflake channelId) where TChannel : class, IGuildChannel
        => this.Bot.GetChannelAsync<TChannel>(channelId);

    /// <summary></summary>
    public async IAsyncEnumerable<GuildThreadChannel> FetchThreadsAsync()
    {
        var activeThreads = await this.Bot.RestClient.GetActiveThreadsAsync(this.Id);
        var threads = activeThreads.GetProperty("threads").EnumerateArray();
        var members = activeThreads.GetProperty("members").EnumerateArray();

        foreach (var thread in threads.Select(json => Common.Entity.Channel.Channel.Create<GuildThreadChannel>(this.Bot, json)))
        {
            foreach (var memberJson in members.Where(json => json.GetProperty("id").GetUInt64() == thread.Id))
                thread.MemberCache.Add(memberJson);

            yield return thread;
        }
    }

    /// <summary></summary>
    public async ValueTask<GuildThreadChannel> GetThreadAsync(Snowflake threadId)
    {
        foreach (var textChannel in this.ChannelCache.OfType<GuildTextChannel>())
        {
            if (textChannel.ThreadCache.TryGet(threadId, out GuildThreadChannel thread))
                return thread;
        }

        return await this.FetchThreadsAsync().FirstOrDefaultAsync(thread => thread.Id == threadId);
    }
}

