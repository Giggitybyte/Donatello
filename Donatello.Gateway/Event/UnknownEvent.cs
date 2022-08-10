namespace Donatello.Gateway.Event;

using System.Text.Json;

/// <summary>An event sent by Discord which is not yet implemented by this library.</summary>
public sealed class UnknownEvent : DiscordEvent
{
    /// <summary>Event name.</summary>
    public string Name { get; internal init; }

    /// <summary>Raw event payload.</summary>
    public JsonElement Json { get; internal init; }
}
