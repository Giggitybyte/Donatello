﻿namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Text.Json;

/// <summary></summary>
public abstract class DiscordEntity : IEquatable<DiscordEntity>
{
    /// <param name="bot">Bot instance to provide convinence methods.</param>
    /// <param name="jsonObject">Backing JSON entity.</param>
    protected DiscordEntity(DiscordApiBot bot, JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException($"Expected JSON object, got {jsonObject.ValueKind}.", nameof(jsonObject));

        this.Bot = bot;
        this.Json = jsonObject;
    }

    /// <summary>Bot instance which contains and manages this object.</summary>
    protected DiscordApiBot Bot { get; private init; }

    /// <summary>Backing JSON object for this entity.</summary>
    protected JsonElement Json { get; private init; }

    /// <summary>Unique snowflake identifier.</summary>
    public virtual ulong Id => this.Json.GetProperty("id").ToUInt64();

    /// <summary>Returns the underlying JSON object for this entity.</summary>
    internal JsonElement GetJson()
        => this.Json;

    public virtual bool Equals(DiscordEntity other)
        => this.Id == other?.Id;

    public override bool Equals(object obj)
        => Equals(obj as DiscordEntity);

    public override int GetHashCode()
        => this.Id.GetHashCode();
}