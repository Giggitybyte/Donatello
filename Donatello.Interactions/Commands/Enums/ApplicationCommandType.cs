namespace Donatello.Interactions.Commands.Enums;

/// <summary>The interaction method of a command.</summary>
public enum ApplicationCommandType
{
    /// <summary>Text-based command which is displayed when a user types <c>/</c></summary>
    Slash = 1,

    /// <summary>UI-based command which is displayed in the user context menu.</summary>
    User = 2,

    /// <summary>UI-based command which is displayed in the message context menu.</summary>
    Message = 3
}
