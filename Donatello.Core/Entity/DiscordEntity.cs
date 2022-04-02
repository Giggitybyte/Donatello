namespace Donatello.Entity;

using System;
using System.Text.Json;

/// <summary></summary>
public abstract class DiscordEntity : IEquatable<DiscordEntity>
{
    internal DiscordEntity(DiscordApiBot bot, JsonElement json)
    {
        this.Bot = bot;
        this.Json = json;
    }

    /// <summary>Bot instance which created this object.</summary>
    protected DiscordApiBot Bot { get; private init; }

    /// <summary>Backing JSON data for this entity.</summary>
    protected JsonElement Json { get; private init; }

    /// <summary>Unique Discord ID.</summary>
    public ulong Id => this.Json.GetProperty("id").ToUInt64();

    public virtual bool Equals(DiscordEntity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => Equals(obj as DiscordEntity);

    public override int GetHashCode()
        => this.Id.GetHashCode();
}