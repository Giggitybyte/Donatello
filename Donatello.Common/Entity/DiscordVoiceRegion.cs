namespace Donatello.Entity;

public struct DiscordVoiceRegion
{
    /// <summary>Snake-case ID string associated with this voice region.</summary>
    public string Identifier { get; internal init; }

    /// <summary>Human-readable name for this voice region.</summary>
    public string Name { get; internal init; }

    /// <summary>Whether this voice region is the closest to the physical location of the executing machine.</summary>
    public bool IsOptimal { get; internal init; }

    /// <summary>Whether this voice region is old and unsupported.</summary>
    public bool IsDeprecated { get; internal init; }

    /// <summary>Whether this voice region a temporary custom region for events and other internal Discord uses.</summary>
    public bool IsCustom { get; internal init; }
}

