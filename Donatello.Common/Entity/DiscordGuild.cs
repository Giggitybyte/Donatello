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
public sealed class DiscordGuild : DiscordEntity
{
    private MemoryCache _memberCache;

    internal DiscordGuild(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        _memberCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>Name of this guild.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Permissions in the guild, excluding channel overwrites.</summary>
    public GuildPermission Permissions => throw new NotImplementedException();

    /// <summary>Cached role instances.</summary>
    public EntityCache<DiscordRole> RoleCache { get; private init; }

    /// <summary>Cached thread channel instances.</summary>
    public EntityCache<DiscordThreadTextChannel> ThreadCache { get; private init; }

    /// <summary>Returns whether this guild has a specific feature enabled.</summary>
    public bool HasFeature(string feature)
        => this.Json.GetProperty("features").EnumerateArray().Any(featureElement => featureElement.GetString() == feature.ToUpper());

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
        if (this.RoleCache.TryGetEntity(roleId, out DiscordRole role))
            return role;
        else
        {
            var jsonRoles = await this.Bot.RestClient.GetGuildRolesAsync(this.Id);

            foreach (var roleJson in jsonRoles.EnumerateArray())
            {
                var updatedRole = new DiscordRole(this.Bot, roleJson);

            }

        }
    }

    /// <summary></summary>
    public Task<DiscordMember> GetMemberAsync(DiscordUser user)
        => GetMemberAsync(user.Id);

    /// <summary></summary>
    public async Task<DiscordMember> GetMemberAsync(DiscordSnowflake userId)
    {
        if (_memberCache.TryGetValue(userId, out JsonElement memberJson) is false)
        {
            memberJson = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            UpdateMemberCache(userId, memberJson);
        }

        var user = await this.Bot.GetUserAsync(userId);
        return new DiscordMember(this.Bot, this.Id, user, memberJson);
    }

    public async IAsyncEnumerable<DiscordMember> GetMembersAsync()
    {

    }

    /// <summary></summary>
    public async ValueTask<EntityCollection<DiscordChannel>> GetChannelsAsync()
    {
        var channelArray = await this.Bot.RestClient.GetGuildChannelsAsync(this.Id);
        var channels = channelArray.EnumerateArray().Select(json => json.ToChannelEntity(this.Bot));
        this.Bot.ChannelCache.ReplaceAll(channels);
        return new EntityCollection<DiscordChannel>(channels);
    }

    /// <summary>Adds the provided <paramref name="memberJson"/> to the member cache.</summary>
    internal void UpdateMemberCache(DiscordSnowflake userId, JsonElement memberJson)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

        _memberCache.Set(userId, memberJson, entryConfig);
    }
}

