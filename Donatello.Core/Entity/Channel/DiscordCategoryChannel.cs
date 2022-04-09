namespace Donatello.Entity;

using Donatello.Rest.Guild;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordCategoryChannel : DiscordChannel
{
    internal DiscordCategoryChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }


    public async Task<EntityCollection<DiscordChannel>> GetChildChannelsAsync()
    {
        var guildId = this.Json.GetProperty("guild_id").ToUInt64();
        var guild = await this.Bot.GetGuildAsync(guildId);
        return new DiscordEntityCollection<DiscordChannel>(guild.GetChannelsAsync())

    }

    /// <summary>Fetches all channels contained in this category.</summary>
    public async Task<EntityCollection<DiscordChannel>> GetChildChannelsAsync()
    {
        var guildId = this.Json.GetProperty("guild_id").ToUInt64();
        var guildChannels = await this.Bot.RestClient.GetGuildChannelsAsync(guildId);

        var childChannels = new DiscordChannel[guildChannels.Payload.GetArrayLength()];
        int arrayIndex = 0;

        foreach (var channel in guildChannels.Payload.EnumerateArray())
            if (channel.TryGetProperty("parent_id", out var property) && property.ToUInt64() == this.Id)
                childChannels[arrayIndex++] = channel.ToChannelEntity(this.Bot);

        return new DiscordEntityCollection<DiscordChannel>(childChannels);
    }
}
