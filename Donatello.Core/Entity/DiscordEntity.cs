namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public abstract class DiscordEntity
{
    internal DiscordEntity(Bot bot, JsonElement json)
    {
        this.Bot = bot;
        this.Json = json;
    }

    /// <summary>Bot instance which created this object.</summary>
    protected Bot Bot { get; private init; }

    /// <summary>Backing JSON data for this entity.</summary>
    protected JsonElement Json { get; private init; }

    /// <summary>Unique Discord ID.</summary>
    public ulong Id => this.Json.GetProperty("id").AsUInt64();
}