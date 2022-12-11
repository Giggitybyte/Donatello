namespace Donatello.Entity;

using Donatello;
using Donatello.Extension.Internal;
using System;
using System.Text.Json;

public abstract class DiscordEntity : ISnowflakeEntity, IBotEntity
{
    private readonly JsonElement _entity;

    protected DiscordEntity(DiscordBot bot, JsonElement entityJson)
    {
        if (entityJson.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {entityJson.ValueKind}.", nameof(entityJson));

        this.Bot = bot;
        _entity = entityJson;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    protected internal JsonElement Json => _entity;    

    /// <inheritdoc cref="IBotEntity.Bot"/>
    protected DiscordBot Bot { get; private init; }

    /// <inheritdoc cref="ISnowflakeEntity.Id"/>
    public virtual DiscordSnowflake Id => this.Json.GetProperty("id").ToSnowflake();

    public override int GetHashCode()
        => this.Id.GetHashCode();

    public virtual bool Equals(DiscordEntity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => this.Equals(obj as DiscordEntity);

    JsonElement IJsonEntity.Json => this.Json;
    DiscordBot IBotEntity.Bot => this.Bot;
    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => this.Equals(other);
}