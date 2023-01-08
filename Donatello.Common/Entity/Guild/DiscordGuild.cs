﻿namespace Donatello.Entity;

using Donatello;
using Donatello.Cache;
using Donatello.Extension.Internal;
using Donatello.Rest.Extension.Endpoint;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A collection of channels and members.</summary>
public partial class DiscordGuild : DiscordEntity
{
    internal DiscordGuild(DiscordBot bot, JsonElement json)
        : base(bot, json)
    {
        this.MemberCache = new JsonCache(json => json.GetProperty("user").GetProperty("id").ToSnowflake());
        this.ChannelCache = new EntityCache<IGuildChannel>();
        this.RoleCache = new EntityCache<DiscordGuildRole>();
        this.ThreadCache = new EntityCache<DiscordThreadChannel>();
    }

    /// <summary>Cached guild member JSON objects.</summary>
    internal JsonCache MemberCache { get; private init; }

    /// <summary>Cached role instances.</summary>
    public EntityCache<DiscordGuildRole> RoleCache { get; private set; }

    /// <summary>Cached channel instances associated with this guild.</summary>
    public EntityCache<IGuildChannel> ChannelCache { get; private set; }

    /// <summary>Cached voice state instances.</summary>
    public ObjectCache<DiscordVoiceState> VoiceStateCache { get; private set; }

    /// <summary>Name of this guild.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Permissions in the guild, excluding channel overwrites.</summary>
    public Permission Permissions => throw new NotImplementedException();

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
    /// <param name="channelTask">
    /// When the method returns:<br/>
    /// - <see langword="true"/> this parameter will conatain a voice channel.<br/>
    /// - <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool HasAfkChannel(out DiscordGuildVoiceChannel voiceChannel)
    {
        // figure out a better construct...
    }

    /// <summary></summary>
    public ValueTask<DiscordUser> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").ToSnowflake());

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildRole> FetchRolesAsync()
    {
        var roles = this.Bot.RestClient.GetGuildRolesAsync(this.Id)
            .Select(json => new DiscordGuildRole(this.Bot, json));

        await foreach (var role in roles)
        {
            this.RoleCache.Add(role.Id, role);
            yield return role;
        }
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<DiscordGuildRole>> GetRolesAsync()
    {
        this.RoleCache.Clear();

        var roles = new List<DiscordGuildRole>();
        await foreach (var role in this.FetchRolesAsync())
            roles.Add(role);

        return roles.AsReadOnly();
    }

    /// <summary></summary>
    public async ValueTask<DiscordGuildRole> GetRoleAsync(DiscordSnowflake roleId)
    {
        if (this.RoleCache.TryGet(roleId, out DiscordGuildRole role) is false)
            role = await this.FetchRolesAsync().SingleOrDefaultAsync(role => role.Id == roleId);

        return role;
    }


    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildMember> FetchMembersAsync()
    {
        await foreach (var memberJson in this.Bot.RestClient.GetGuildMembersAsync(this.Id))
        {
            var userId = memberJson.GetProperty("id").ToSnowflake();
            this.MemberCache.Add(userId, memberJson);

            var user = await this.Bot.GetUserAsync(userId);
            yield return new DiscordGuildMember(this.Bot, this.Id, user, memberJson);
        }
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<DiscordGuildMember>> GetMembersAsync()
    {
        this.MemberCache.Clear();

        var members = new List<DiscordGuildMember>();
        await foreach (var member in this.FetchMembersAsync())
            members.Add(member);

        return members.AsReadOnly();
    }

    /// <summary></summary>
    public async ValueTask<DiscordGuildMember> GetMemberAsync(DiscordSnowflake userId)
    {
        if (this.MemberCache.TryGet(userId, out JsonElement memberJson) is false)
        {
            memberJson = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            this.MemberCache.Add(userId, memberJson);
        }

        var user = await this.Bot.GetUserAsync(userId);
        return new DiscordGuildMember(this.Bot, this.Id, user, memberJson);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildTextChannel> FetchChannelsAsync()
    {
        var channels = this.Bot.RestClient.GetGuildChannelsAsync(this.Id)
            .Select(channelJson => DiscordChannel.Create<DiscordGuildTextChannel>(channelJson, this.Bot));

        await foreach (var channel in channels)
            yield return channel;
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<DiscordGuildTextChannel>> GetChannelsAsync()
    {
        this.ChannelCache.Clear();

        var channels = new List<DiscordGuildTextChannel>();
        await foreach (var channel in this.FetchChannelsAsync())
        {
            channels.Add(channel);
            this.ChannelCache.Add(channel);
        }

        return channels.AsReadOnly();
    }

    /// <inheritdoc cref="DiscordBot.GetChannelAsync(DiscordSnowflake)"/>
    public ValueTask<TChannel> GetChannelAsync<TChannel>(DiscordSnowflake channelId) where TChannel : class, IGuildChannel
        => this.Bot.GetChannelAsync<TChannel>(channelId);

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordThreadChannel> FetchThreadsAsync()
    {
        var activeThreads = await this.Bot.RestClient.GetActiveThreadsAsync(this.Id);
        var threads = activeThreads.GetProperty("threads").EnumerateArray();
        var members = activeThreads.GetProperty("members").EnumerateArray();

        foreach (var thread in threads.Select(json => DiscordChannel.Create<DiscordThreadChannel>(json, this.Bot)))
        {
            foreach (var memberJson in members.Where(json => json.GetProperty("id").GetUInt64() == thread.Id))
                thread.MemberCache.Add(memberJson);

            yield return thread;
        }
    }

    /// <summary></summary>
    public async Task<ReadOnlyCollection<DiscordThreadChannel>> GetThreadsAsync()
    {
        this.ThreadCache.Clear();

        var threads = new List<DiscordThreadChannel>();
        await foreach (var thread in this.FetchThreadsAsync())
        {
            threads.Add(thread);
            this.ThreadCache.Add(thread);
        }

        return threads.AsReadOnly();
    }

    /// <summary></summary>
    public async ValueTask<DiscordThreadChannel> GetThreadAsync(DiscordSnowflake threadId)
    {
        if (this.ThreadCache.TryGet(threadId, out DiscordThreadChannel thread) is false)
            thread = await this.FetchThreadsAsync().SingleOrDefaultAsync(thread => thread.Id == threadId);

        return thread;
    }
}

