namespace Donatello.Enum;

using System;

/// <summary>Additional message metadata.</summary>
[Flags]
public enum MessageFlag : int
{
    /// <summary>Message has been published to all channels subscribed to the parent channel.</summary>
    Crossposted = 1 << 0,

    /// <summary>Message originates from an announcement channel in another server.</summary>
    Crosspost = 1 << 1,

    /// <summary>Embeds will not be displayed with the message.</summary>
    SuppressEmbeds = 1 << 2,

    /// <summary>Message refers to an announcement which has been deleted.</summary>
    SourceMessageDeleted = 1 << 3,

    /// <summary>Message is from the Discord urgent message system.</summary>
    SystemMessage = 1 << 4,

    /// <summary>Message has a thread channel associated with it.</summary>
    HasThread = 1 << 5,

    /// <summary>Message is only visible to its author.</summary>
    Ephemeral = 1 << 6,

    /// <summary>Message is a placeholder interaction response.</summary>
    Thinking = 1 << 7
}
