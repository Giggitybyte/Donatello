namespace Donatello.Interaction;

using System;
using System.Text.Json;
using Common;
using Common.Entity;

public abstract class DiscordInteraction : IInteraction
{
    JsonElement IJsonEntity.Json => throw new NotImplementedException();

    bool IEquatable<ISnowflakeEntity>.Equals(ISnowflakeEntity other) => throw new NotImplementedException();

    Snowflake ISnowflakeEntity.Id => throw new NotImplementedException();

    Snowflake IInteraction.ApplicationId => throw new NotImplementedException();

    int IInteraction.Type => throw new NotImplementedException();

    string IInteraction.Token => throw new NotImplementedException();

    int IInteraction.Version => throw new NotImplementedException();
}