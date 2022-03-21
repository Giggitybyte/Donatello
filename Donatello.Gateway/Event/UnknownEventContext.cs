namespace Donatello.Gateway.Event;

using System.Text.Json;

/// <summary></summary>
public class UnknownEventContext : DiscordEventContext
{
    internal UnknownEventContext(string eventName, JsonElement eventJson) : base(eventJson)
    {
        this.Name = eventName;
    }

    /// <summary></summary>
    public string Name { get; private init; }

    /// <summary></summary>
    public JsonElement Payload { get => this.Json; }
}
