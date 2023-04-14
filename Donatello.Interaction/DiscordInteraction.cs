namespace Donatello.Interaction;

using System;
using System.Text.Json;
using Common;
using Common.Entity;

public abstract class DiscordInteraction : IInteractionEntity
{
    JsonElement IJsonEntity.Json => throw new NotImplementedException();

    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => throw new NotImplementedException();

    Snowflake ISnowflakeEntity.Id => throw new NotImplementedException();

    Snowflake IInteractionEntity.ApplicationId => throw new NotImplementedException();

    int IInteractionEntity.Type => throw new NotImplementedException();

    string IInteractionEntity.Token => throw new NotImplementedException();

    int IInteractionEntity.Version => throw new NotImplementedException();
}