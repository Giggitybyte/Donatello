namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordGuildTextChannel : DiscordTextChannel
{
    public DiscordGuildTextChannel(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        this.ThreadCache = new EntityCache<DiscordThreadTextChannel>(TimeSpan.FromHours(1));
    }

    /// <summary>Cached thread channel instances.</summary>
    public EntityCache<DiscordThreadTextChannel> ThreadCache { get; private set; }

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.Json.GetProperty("guild_id").ToSnowflake());
}


