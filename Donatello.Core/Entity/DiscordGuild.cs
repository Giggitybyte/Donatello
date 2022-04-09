namespace Donatello.Entity;

using Donatello.Extension.Internal;
using Donatello.Rest.Guild;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A collection of channels and members.</summary>
public sealed class DiscordGuild : DiscordEntity
{
    private MemoryCache _memberCache, _channelCache, _roleCache, _emojiCache;

    public DiscordGuild(DiscordApiBot bot, JsonElement json) : base(bot, json)
    {
        _memberCache = new MemoryCache(new MemoryCacheOptions());
        _channelCache = new MemoryCache(new MemoryCacheOptions());
        _roleCache = new MemoryCache(new MemoryCacheOptions());
        _emojiCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>The guild name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Returns whether this guild has a specific feature enabled.</summary>
    public bool HasFeature(string feature)
    {
        var features = this.Json.GetProperty("features");

        if (features.GetArrayLength() is not 0)
        {
            foreach (var featureElement in features.EnumerateArray())
                if (featureElement.GetString() == feature.ToUpper())
                    return true;
        }

        return false;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an icon image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="iconUrl">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the icon URL.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasIcon(out string iconUrl)
    {
        var iconProp = this.Json.GetProperty("icon");

        if (iconProp.ValueKind is not JsonValueKind.Null)
        {
            var iconHash = iconProp.GetString();
            var extension = iconHash.StartsWith("a_") ? "gif" : "png";
            iconUrl = $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";

            return true;
        }
        else
        {
            iconUrl = string.Empty;
            return false;
        }
    }

    /// <summary>Returns <see langword="true"/> if the guild has an invite splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will conatain the invite splash URL.<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasInviteSplash(out string splashUrl)
    {
        if (this.Json.TryGetProperty("discovery_splash", out var property) && property.ValueKind is not JsonValueKind.Null)
        {
            splashUrl = $"https://cdn.discordapp.com/splashes/{this.Id}/{property.GetString()}.png";
            return true;
        }
        else
        {
            splashUrl = string.Empty;
            return false;
        }
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
        {
            splashUrl = $"https://cdn.discordapp.com/splashes/{this.Id}/{property.GetString()}.png";
            return true;
        }
        else
        {
            splashUrl = string.Empty;
            return true;
        }
    }



    /// <summary></summary>
    public ValueTask<DiscordUser> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").ToUInt64());

    /// <summary></summary>
    public async ValueTask<DiscordRole> GetRoleAsync(ulong roleId)
    {
        if (_roleCache.TryGetValue(roleId, out DiscordRole role))
            return role;
        else
        {
            var json = await this.Bot.RestClient.GetGuildRolesAsync(this.Id);

        }
    }

    /// <summary></summary>
    public async ValueTask<DiscordMember> GetMemberAsync(ulong userId)
    {
        if (_memberCache.TryGetValue(userId, out DiscordMember member))
            return member;
        else
        {
            var json = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            var member = new DiscordMember(this.Bot, json, userId);
        }
    }

    /// <summary></summary>
    public ValueTask<DiscordMember> GetMemberAsync(DiscordUser user)
        => GetMemberAsync(user.Id);

    /// <summary></summary>
    public async ValueTask<EntityCollection<DiscordChannel>> GetChannelsAsync()
    {

        var response = await this.Bot.RestClient.GetGuildChannelsAsync(this.Id);
        var channels = new DiscordChannel[response.Payload.GetArrayLength()];

        int index = 0;
        foreach (var channelJson in response.Payload.EnumerateArray())
        {
            var channel = channelJson.ToChannelEntity(this.Bot);
            channels[index++] = channel;

        }

        return new EntityCollection<DiscordChannel>(channels);
    }

    /// <summary></summary>
    internal void UpdateChannelCache(DiscordChannel channel)
    {
        var entryConfig = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1))
            .RegisterPostEvictionCallback(LogChannelCacheEviction);

        _channelCache.Set(channel.Id, channel, entryConfig);

        void LogChannelCacheEviction(object key, object value, EvictionReason reason, object state)
            => this.Bot.Logger.LogTrace("Removed entry {Id} from the channel cache ({Reason})", (ulong)key, reason);
    }
}

