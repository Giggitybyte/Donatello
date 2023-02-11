namespace Donatello.Entity;

using Donatello;
using Extension.Internal;
using System;
using System.Text.Json;

public abstract class Entity : ISnowflakeEntity, IBotEntity
{
    protected Entity(Bot bot, JsonElement entityJson)
    {
        if (entityJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {entityJson.ValueKind}.", nameof(entityJson));

        this.Bot = bot;
        this.Json = entityJson;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    protected internal JsonElement Json { get; private set; }

    /// <inheritdoc cref="IBotEntity.Bot"/>
    protected Bot Bot { get; private init; }

    /// <inheritdoc cref="ISnowflakeEntity.Id"/>
    public virtual Snowflake Id => this.Json.GetProperty("id").ToSnowflake();

    /// <summary>Replaces the backing <see cref="JsonElement"/> of this entity with the provided instance.</summary>
    protected internal void Update(JsonElement updatedJson)
    {
        if (updatedJson.ValueKind is not JsonValueKind.Object)
            throw new JsonException($"Expected JSON object, got {updatedJson.ValueKind.ToString().ToLower()} instead.");
        
        if (updatedJson.TryGetProperty("id", out JsonElement snowflakeJson) is false)
            throw new JsonException("Key 'id' is not present; provided object is invalid.");

        if (snowflakeJson.ToSnowflake() != this.Id)
            throw new InvalidOperationException("ID mismatch; provided entity does not represent this entity.");

        this.Json = updatedJson;
    }

    public override int GetHashCode()
        => this.Id.GetHashCode();

    public virtual bool Equals(Entity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => obj is Entity entity && this.Equals(entity);

    /// <summary>Returns the JSON representation of this entity.</summary>
    public override string ToString()
        => this.Json.ToString();

    public static implicit operator Snowflake(Entity entity)
        => entity.Id;

    JsonElement IJsonEntity.Json => this.Json;
    void IJsonEntity.Update(JsonElement updatedJson) => this.Update(updatedJson);
    Bot IBotEntity.Bot => this.Bot;
    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => this.Equals(other);
}