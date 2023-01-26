namespace Donatello.Entity;

using Donatello;
using Donatello.Extension.Internal;
using System;
using System.Text.Json;

public abstract class Entity : ISnowflakeEntity, IBotEntity
{
    private readonly JsonElement _entity;

    protected Entity(Bot bot, JsonElement entityJson)
    {
        if (entityJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {entityJson.ValueKind}.", nameof(entityJson));

        this.Bot = bot;
        _entity = entityJson;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    protected internal JsonElement Json => _entity;    

    /// <inheritdoc cref="IBotEntity.Bot"/>
    protected Bot Bot { get; private init; }

    /// <inheritdoc cref="ISnowflakeEntity.Id"/>
    public virtual Snowflake Id => this.Json.GetProperty("id").ToSnowflake();

    public override int GetHashCode()
        => this.Id.GetHashCode();

    public virtual bool Equals(Entity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => obj is Entity && this.Equals(obj as Entity);

    /// <summary>Returns the string representation of this entity.</summary>
    public override string ToString()
        => this.Json.ToString();

    public static implicit operator Snowflake(Entity entity)
        => entity.Id;

    JsonElement IJsonEntity.Json => this.Json;
    Bot IBotEntity.Bot => this.Bot;
    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => this.Equals(other);
}