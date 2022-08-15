namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;

public abstract class DiscordEntity : IEntity
{
    /// <param name="bot">Bot instance to provide convinence methods.</param>
    /// <param name="jsonObject">Backing JSON entity.</param>
    protected DiscordEntity(DiscordBot bot, JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {jsonObject.ValueKind}.", nameof(jsonObject));

        this.Bot = bot;
        this.Json = jsonObject;
    }

    /// <summary>Bot instance which contains and manages this object.</summary>
    protected DiscordBot Bot { get; private init; }

    /// <summary>Backing JSON object for this entity.</summary>
    protected internal JsonElement Json { get; private init; }

    /// <inheritdoc/>
    public virtual DiscordSnowflake Id => this.Json.GetProperty("id").ToSnowflake();

    public virtual bool Equals(DiscordEntity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => Equals(obj as DiscordEntity);

    public override int GetHashCode()
        => this.Id.GetHashCode();

    IBot IEntity.Bot => this.Bot;
    JsonElement IEntity.Json => this.Json;
    bool IEquatable<IEntity>.Equals(IEntity other) => this.Equals(other);
}