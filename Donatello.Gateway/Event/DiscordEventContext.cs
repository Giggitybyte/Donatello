namespace Donatello.Gateway.Event;

using System;
using System.Text.Json;

public abstract class DiscordEventContext : EventArgs
{
    internal DiscordEventContext(JsonElement eventJson)
    {
        this.Json = eventJson;
    }

    /// <summary>JSON payload backing this object.</summary>
    protected JsonElement Json { get; private init; }
}
