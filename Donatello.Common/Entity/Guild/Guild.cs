namespace Donatello.Common.Entity.Guild;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Donatello.Rest.Extension.Endpoint;
using Enum;
using Extension;
using User;

/// <summary>A collection of channels and members.</summary>
public class Guild : Entity
{
    public Guild(JsonElement json, Bot bot) : base(json, bot)
    {
        
    }

    /// <summary>Name of this guild.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Permissions in the guild, excluding channel overwrites.</summary>
    public GuildPermission Permissions => throw new NotImplementedException();

    /// <summary>Snowflake ID for the user which owns this guild.</summary>
    public Snowflake OwnerId => this.Json.GetProperty("owner_id").ToSnowflake();

    /// <summary>Returns whether this guild has a specific feature enabled.</summary>
    public bool HasFeature(string feature)
        => this.Json.GetProperty("features").EnumerateArray().Any(guildFeature => guildFeature.GetString() == feature.ToUpper());

    /// <summary>Returns <see langword="true"/> if the guild has an icon image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="iconUrl">If the method returns <see langword="true"/> this parameter will contain the icon URL; otherwise it will be <see cref="string.Empty"/>.</param>
    public bool HasIcon(out string iconUrl)
    {
        if (this.Json.TryGetProperty("icon", out var prop) && prop.ValueKind is not JsonValueKind.Null)
        {
            var iconHash = prop.GetString();
            var extension = iconHash!.StartsWith("a_") ? "gif" : "png";
            iconUrl = $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";
        }
        else
            iconUrl = string.Empty;

        return iconUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an invite splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">If the method returns <see langword="true"/> this parameter will contain the invite splash URL; otherwise it will be <see cref="string.Empty"/>.</param>
    public bool HasInviteSplash(out string splashUrl)
    {
        splashUrl = (this.Json.TryGetProperty("splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            ? $"https://cdn.discordapp.com/splashes/{this.Id}/{property.GetString()}.png"
            : string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has a discovery splash image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="splashUrl">If the method returns <see langword="true"/> this parameter will contain the discovery splash URL; otherwise it will be <see cref="string.Empty"/>.</param>
    public bool HasDiscoverySplash(out string splashUrl)
    {
        splashUrl = (this.Json.TryGetProperty("discovery_splash", out var property) && property.ValueKind is not JsonValueKind.Null)
            ? $"https://cdn.discordapp.com/discovery-splashes/{this.Id}/{property.GetString()}.png"
            : string.Empty;

        return splashUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the guild has an AFK voice channel set, <see langword="false"/> otherwise.</summary>
    /// <param name="channelId">
    /// If the method returns <see langword="true"/> this parameter will contain the
    /// <see cref="Snowflake"/> ID of the AFK channel, otherwise it will be <see langword="null"/>.
    /// </param>
    public bool HasAfkChannel(out Snowflake channelId)
    {
        channelId = this.Json.GetProperty("afk_channel_id").ToSnowflake();
        return channelId is not null;
    }

    /// <summary></summary>
    public ValueTask<User> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.OwnerId);

    /// <summary></summary>
    public async IAsyncEnumerable<Role> FetchRolesAsync()
    {
        await foreach (var roleJson in this.Bot.RestClient.GetGuildRolesAsync(this.Id))
        {
            this.Bot.GuildRoleCache[this.Id].AddOrUpdate(roleJson);
            yield return new Role(this.Bot, roleJson);
        }
    }

    /// <summary></summary>
    public async ValueTask<Role> GetRoleAsync(Snowflake roleId)
    {
        if (this.Bot.GuildRoleCache[this.Id].TryGetEntry(roleId, out JsonElement roleJson) is false)
            roleJson = await this.Bot.RestClient.GetGuildRolesAsync(this.Id).FirstOrDefaultAsync(json => roleId == json.GetProperty("id").ToSnowflake());

        if (roleJson.ValueKind is not JsonValueKind.Undefined)
            return new Role(this.Bot, roleJson);
        else
            throw new ArgumentException("Invalid role ID", nameof(roleId));
    }

    /// <summary></summary>
    public async IAsyncEnumerable<GuildMember> FetchMembersAsync()
    {
        this.Bot.GuildMemberCache[this.Id].Clear();

        await foreach (var memberJson in this.Bot.RestClient.GetGuildMembersAsync(this.Id))
        {
            this.Bot.GuildMemberCache[this.Id].AddOrUpdate(memberJson);
            yield return new GuildMember(memberJson, this.Id, this.Bot);
        }
    }

    /// <summary></summary>
    public async ValueTask<GuildMember> GetMemberAsync(Snowflake userId)
    {
        if (this.Bot.GuildMemberCache[this.Id].TryGetEntry(userId, out JsonElement memberJson) is false)
        {
            memberJson = await this.Bot.RestClient.GetGuildMemberAsync(this.Id, userId);
            this.Bot.GuildMemberCache[this.Id].AddOrUpdate(userId, memberJson);
        }
        
        return new GuildMember(memberJson, this.Id, this.Bot);
    }

    /// <summary></summary>
    public async IAsyncEnumerable<GuildChannel> FetchChannelsAsync()
    {
        await foreach (var channelJson in this.Bot.RestClient.GetGuildChannelsAsync(this.Id))
        {
            this.Bot.ChannelCache.AddOrUpdate(channelJson);
            yield return channelJson.AsChannel<GuildChannel>(this.Bot);
        }
    }

    /// <inheritdoc cref="Bot.GetChannelAsync(Snowflake)"/>
    public ValueTask<TChannel> GetChannelAsync<TChannel>(Snowflake channelId) where TChannel : GuildChannel
        => this.Bot.GetChannelAsync<TChannel>(channelId);

    /// <summary></summary>
    public async IAsyncEnumerable<GuildThreadChannel> FetchAllThreadsAsync()
    {
        var activeThreads = await this.Bot.RestClient.GetActiveThreadsAsync(this.Id);
        var jsonThreads = activeThreads.GetProperty("threads");
        var jsonMembers = activeThreads.GetProperty("members");
        var threads = new List<GuildThreadChannel>(jsonThreads.GetArrayLength());

        foreach (var threadJson in jsonThreads.EnumerateArray())
        {
            var thread = threadJson.AsChannel<GuildThreadChannel>(this.Bot);
            
            foreach (var memberJson in jsonMembers.EnumerateArray())
            {
                if (memberJson.GetProperty("id").GetUInt64() != thread.Id) continue;
                this.Bot.ThreadMemberCache[thread.Id].AddOrUpdate(memberJson.GetProperty("user_id").ToSnowflake(), memberJson);
            }

            this.Bot.GuildThreadCache[this.Id].AddOrUpdate(thread.Json);
            threads.Add(thread);
        }

        foreach (var thread in threads.OrderByDescending(thread => thread.Id.CreationDate))
            yield return thread;
    }

    /// <summary></summary>
    public ValueTask<GuildThreadChannel> GetThreadAsync(Snowflake threadId)
    {
        if (this.Bot.GuildThreadCache[this.Id].TryGetEntry(threadId, out JsonElement threadJson))
            return ValueTask.FromResult(new GuildThreadChannel(threadJson, this.Bot));
        else
            return this.FetchAllThreadsAsync().FirstOrDefaultAsync(thread => thread.Id == threadId);
    }
}

