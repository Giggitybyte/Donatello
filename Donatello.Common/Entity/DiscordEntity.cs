namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;

public abstract class DiscordEntity : IEntity
{
    private readonly JsonElement _entity;

    /// <param name="bot">Bot instance to provide convinence methods.</param>
    /// <param name="entityJson">Backing JSON entity.</param>
    protected DiscordEntity(DiscordBot bot, JsonElement entityJson)
    {
        if (entityJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {entityJson.ValueKind}.", nameof(entityJson));

        this.Bot = bot;
        _entity = entityJson;
    }

    JsonElement IEntity.Json => this.Json;
    DiscordBot IEntity.Bot => this.Bot;
    bool IEquatable<IEntity>.Equals(IEntity other) => this.Equals(other);

    /// <summary>Backing JSON object for this entity.</summary>
    protected internal JsonElement Json => _entity;

    /// <summary>Bot instance which contains and manages this object.</summary>
    protected DiscordBot Bot { get; private init; }

    /// <inheritdoc/>
    public virtual DiscordSnowflake Id => this.Json.GetProperty("id").ToSnowflake();

    public virtual bool Equals(DiscordEntity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => this.Equals(obj as DiscordEntity);

    public override int GetHashCode()
        => this.Id.GetHashCode();
}