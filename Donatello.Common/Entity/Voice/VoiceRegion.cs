namespace Donatello.Entity;

using System.Text.Json;

public sealed class VoiceRegion : IJsonEntity
{
    /// <inheritdoc cref="IJsonEntity.Json"/>
    internal JsonElement Json { get; set; }

    /// <summary>Snake-case ID string associated with this voice region.</summary>
    public string Identifier => this.Json.GetProperty("id").GetString();

    /// <summary>Human-readable name for this voice region.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Whether this voice region is the closest to the physical location of the executing machine.</summary>
    public bool IsOptimal => this.Json.GetProperty("optimal").GetBoolean();

    /// <summary>Whether this voice region is old or unsupported.</summary>
    public bool IsDeprecated => this.Json.GetProperty("deprecated").GetBoolean();

    /// <summary>Whether this voice region a temporary custom region for events and other internal Discord uses.</summary>
    public bool IsCustom => this.Json.GetProperty("custom").GetBoolean();

    JsonElement IJsonEntity.Json => this.Json;
}

