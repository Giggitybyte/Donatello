namespace Donatello.Core.Entity;

using System.Text.Json;

/// <summary></summary>
public abstract class DiscordEntity
{
    internal DiscordEntity(AbstractBot bot, JsonElement json)
    {
        this.Bot = bot;
        this.Json = json;
    }

    /// <summary>Bot instance which created this object.</summary>
    protected AbstractBot Bot { get; private init; }

    /// <summary>Backing JSON data for this entity.</summary>
    protected JsonElement Json { get; private init; }

    /// <summary>Unique Discord ID.</summary>
    public ulong Id => this.Json.GetProperty("id").AsUInt64();
}