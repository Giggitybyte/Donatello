﻿namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Extension;
using Donatello.Rest.Endpoint;
using Qommon.Collections;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public sealed class DiscordCategoryChannel : DiscordChannel
{
    internal DiscordCategoryChannel(DiscordBot bot, JsonElement json) : base(bot, json) { }

    public async Task<ReadOnlyList<DiscordChannel>> GetChildChannelsAsync()
    {
        var guildId = this.Json.GetProperty("guild_id").AsUInt64();
        var guildChannels = await this.Bot.HttpClient.GetGuildChannelsAsync(guildId);

        var childChannels = new DiscordChannel[guildChannels.Payload.GetArrayLength()];
        int arrayIndex = 0;

        foreach (var channel in guildChannels.Payload.EnumerateArray())
        {
            if (channel.TryGetProperty("parent_id", out var prop) && (prop.AsUInt64() == this.Id))
                childChannels[arrayIndex++] = channel.ToEntity<DiscordChannel>(this.Bot);
        }

        return new ReadOnlyList<DiscordChannel>(childChannels);
    }
}
