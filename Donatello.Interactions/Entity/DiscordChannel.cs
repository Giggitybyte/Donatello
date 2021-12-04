namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Entity.Enumeration;
using Donatello.Interactions.Extension;
using Donatello.Rest.Extension.Endpoint;
using Qommon.Collections;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public abstract class DiscordChannel : DiscordEntity
{
    internal DiscordChannel(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        var jsonType = json.GetProperty("type").GetInt32();
        if (Enum.IsDefined(typeof(ChannelType), jsonType))
            this.Type = (ChannelType)jsonType;
        else
            throw new JsonException("Unknown channel type.");
    }

    /// <summary>Type of this channel.</summary>
    internal ChannelType Type { get; private init; }
}

/// <summary>Text channel between two or more users.</summary>
public sealed class DiscordDirectChannel : DiscordChannel
{
    public DiscordDirectChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Channel icon URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if this channel does not have an icon.</remarks>
    public string IconUrl => this.Json.TryGetProperty("icon", out var prop) ? $"https://cdn.discordapp.com/channel-icons/{this.Id}/{prop.GetString()}.png" : string.Empty;

    public ReadOnlyList<DiscordUser> Users
    {
        get
        {
            var user = this.Json.GetProperty("").ToEntityArray<DiscordUser>(this.Bot);
            return new ReadOnlyList<DiscordUser>(user);
        }
    }
}

/// <summary>Text channel within a guild.</summary>
public class DiscordTextChannel : DiscordChannel
{
    public DiscordTextChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Name of the channel.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary></summary>
    public string Topic => this.Json.GetProperty("topic").GetString();

    /// <summary></summary>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary>Amount of time a user must wait before sending another message.</summary>
    public TimeSpan SlowmodeWaitTime => TimeSpan.FromSeconds(this.Json.GetProperty("rate_limit_per_user").GetInt32());

    /// <summary>Whether the channel has the potential to contain NSFW content.</summary>
    public bool IsNsfw => this.Json.GetProperty("nsfw").GetBoolean();

    /// <summary></summary>
    //public object[] PermissionOverwrites { get; private set; }

    /// <summary></summary>
    public Task<DiscordChannel> GetLastMessageAsync()
    {
        var id = this.Json.GetProperty("last_message_id").AsUInt64();
        return this.Bot.GetChannelAsync(id);

    }

    /// <summary></summary>
    public Task<DiscordGuild> GetGuildAsync()
    {
        var id = this.Json.GetProperty("guild_id").AsUInt64();
        return this.Bot.GetGuildAsync(id);
    }
}

/// <summary>Temporary sub-channel within a text channel.</summary>
public sealed class DiscordThreadChannel : DiscordTextChannel
{
    public DiscordThreadChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public int MessageCount => this.Json.GetProperty("message_count").GetInt32();

}

/// <summary>Voice channel within a guild.</summary>
public class DiscordVoiceChannel : DiscordChannel
{
    public DiscordVoiceChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }


}

/// <summary></summary>
public sealed class DiscordStageChannel : DiscordVoiceChannel
{
    public DiscordStageChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }
}

/// <summary></summary>
public sealed class DiscordCategoryChannel : DiscordChannel
{
    public DiscordCategoryChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    public async Task<ReadOnlyList<DiscordChannel>> GetChildChannelsAsync()
    {
        var guildId = this.Json.GetProperty("guild_id").AsUInt64();
        var channels = await this.Bot.HttpClient.GetGuildChannelsAsync(guildId);
    }
}