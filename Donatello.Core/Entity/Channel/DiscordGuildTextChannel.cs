namespace Donatello.Entity;

using Donatello.Rest.Channel;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public class DiscordGuildTextChannel : DiscordTextChannel
{
    public DiscordGuildTextChannel(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

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
    public Task BulkDeleteMessagesAsync(IEnumerable<DiscordMessage> messages)
    {
        var messageIds = new List<ulong>();
        foreach (var msg in messages)
            messageIds.Add(msg.Id);

        return this.Bot.RestClient.BulkDeleteMessagesAsync(this.Id, messageIds);
    }
}
