namespace Donatello.Enum;

/// <summary></summary>
public enum ModerationTriggerType : ushort
{
    /// <summary>User-defined list of keywords.</summary>
    UserKeywords = 1,

    /// <summary>Common malicious URLs.</summary>
    Url = 2,

    /// <summary>Common variations of spam.</summary>
    Spam = 3,

    /// <summary>List of keywords defined by Discord.</summary>
    PresetKeywords = 4
}

