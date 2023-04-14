namespace Donatello.Common.Entity;

using System;
using System.Text.Json;
using Extension;

public abstract class Entity : ISnowflakeEntity, IBotEntity
{
    protected Entity(JsonElement entityJson, Bot bot)
    {
        if (entityJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {entityJson.ValueKind}.", nameof(entityJson));

        this.Bot = bot;
        this.Json = entityJson;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    protected internal JsonElement Json { get; }

    /// <inheritdoc cref="IBotEntity.Bot"/>
    protected Bot Bot { get; }

    /// <inheritdoc cref="ISnowflakeEntity.Id"/>
    public virtual Snowflake Id => this.Json.GetProperty("id").ToSnowflake();

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
    Bot IBotEntity.Bot => this.Bot;
    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => this.Equals(other);
}