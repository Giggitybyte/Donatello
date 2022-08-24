namespace Donatello.Entity;

using Donatello.Enumeration;
using Donatello.Extension.Internal;
using Donatello.Rest.Extension.Endpoint;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A collection of channels and members.</summary>
public class DiscordGuild : DiscordEntity
{
    private ObjectCache<JsonElement> _memberCache;

    internal DiscordGuild(DiscordBot bot, JsonElement json) 
        : base(bot, json)
    {
        _memberCache = new ObjectCache<JsonElement>();
        this.RoleCache = new ObjectCache<DiscordRole>();
        this.ThreadCache = new ObjectCache<DiscordThreadTextChannel>();
    }

    /// <summary>Name of this guild.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Cached thread channel instances.</summary>
    public ObjectCache<DiscordThreadTextChannel> ThreadCache { get; private set; }

    /// <summary></summary>
    public ObjectCache<DiscordRole> RoleCache { get; private set; }

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
    /// <see langword="true"/> this parameter will conatain the invite splash URL.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasInviteSplash(out string splashUrl)
    {
        if (this.Json.TryGetProperty("splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            splashUrl = $"https://cdn.discordapp.com/splashes/{this.Id}/{property.GetString()}.png";
        else
            splashUrl = string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an discovery splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will conatain the discovery splash URL.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasDiscoverySplash(out string splashUrl)
    {
        if (this.Json.TryGetProperty("discovery_splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            splashUrl = $"https://cdn.discordapp.com/discovery-splashes/{this.Id}/{property.GetString()}.png";
        else
            splashUrl = string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an AFK voice channel set, <see langword="false"/> otherwise.</summary>
    /// <param name="afkChannel">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will conatain a voice channel.<br/>
    /// <see langword="false"/> this parameter will be <see langword="null"/>.
    /// </param>
    public bool HasAfkChannel(out DiscordVoiceChannel afkChannel)
    {

    }

    /// <summary></summary>
    public ValueTask<DiscordUser> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").ToSnowflake());

    /// <summary></summary>
    public async ValueTask<DiscordRole> GetRoleAsync(DiscordSnowflake roleId)
    {
        if (this.RoleCache.Contains(roleId, out DiscordRole role) is false)
        {
            var jsonRoles = await this.Bot.RestClient.GetGuildRolesAsync(this.Id);
            foreach (var roleJson in jsonRoles.EnumerateArray())
            {
                role = new DiscordRole(this.Bot, roleJson);
                this.RoleCache.Add(roleId, role);
            }
        }

        return role;
    }

    /// <summary></summary>
    public Task<DiscordGuildMember> GetMemberAsync(DiscordUser user)
        => this.GetMemberAsync(user.Id);

    /// <summary></summary>
    public async Task<DiscordGuildMember> GetMemberAsync(DiscordSnowflake userId)
    {
        if (_memberCache.Contains(userId, out JsonElement memberJson) is false)
        {
            memberJson = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            _memberCache.Add(userId, memberJson);
        }

        var user = await this.Bot.GetUserAsync(userId);
        return new DiscordGuildMember(this.Bot, this.Id, user, memberJson);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordGuildMember> GetMembersAsync()
    {
        var memberArray = await this.Bot.RestClient.GetGuildMembersAsync(this.Id);
        _memberCache.Clear();

        foreach (var memberJson in memberArray.EnumerateArray())
        {
            var userId = memberJson.GetProperty("id").ToSnowflake();
            _memberCache.Add(userId, memberJson);

            var user = await this.Bot.GetUserAsync(userId);
            yield return new DiscordGuildMember(this.Bot, this.Id, user, memberJson);
        }
    }

    /// <summary></summary>
    public async IAsyncEnumerable<DiscordChannel> GetChannelsAsync()
    {
        var channelArray = await this.Bot.RestClient.GetGuildChannelsAsync(this.Id);
        var channels = channelArray.EnumerateArray().Select(json => json.ToChannelEntity(this.Bot));

        this.Bot.ChannelCache.Clear();

        foreach (var channel in channels)
        {
            this.Bot.ChannelCache.Add(channel.Id, channel);
            yield return channel;
        }
    }
}

