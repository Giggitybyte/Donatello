namespace Donatello.Gateway.Event;

using System.Threading.Tasks;
using Entity;

/// <summary>Guild related event received by a websocket shard.</summary>
public abstract class GuildEvent : ShardEvent
{
    /// <summary>ID of the guild which this dispatched event.</summary>
    public Snowflake GuildId { get; internal init;}

    /// <summary>Attempts to get the guild which dispatched this event from the cache;
    /// if the guild is not cached, an up-to-date guild will be fetched from Discord.</summary>
    public ValueTask<Guild> GetGuildAsync()
        => this.Bot.GetGuildAsync(this.GuildId);
}