namespace Donatello.Interactions.Entity;

using System.Text.Json;

public abstract class DiscordEntity
{
    internal DiscordEntity(DiscordBot bot, JsonElement json)
    {
        this.Bot = bot;
        this.Json = json;        
    }

    /// <summary>Bot instance which created this object.</summary>
    protected DiscordBot Bot { get; private init; }

    /// <summary>Backing JSON data for this entity.</summary>
    protected JsonElement Json { get; private init; }

    /// <summary>(Mostly) unique Discord ID.</summary>
    public ulong Id => this.Json.GetProperty("id").AsUInt64();
}
