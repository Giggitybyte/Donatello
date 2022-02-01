namespace Donatello.Interactions.Entity.Enumeration;

internal enum ChannelType
{
    /// <summary>Text channel within a guild.</summary>
    Text = 0,

    /// <summary>Text channel between two users.</summary>
    Direct = 1,

    /// <summary>Voice channel within a server.</summary>
    Voice = 2,

    /// <summary>Direct text channel between multiple users.</summary>
    Group = 3,

    /// <summary>Organizational channel which can contain up to 50 child channels.</summary>
    Category = 4,

    /// <summary>Server text channel that users can follow and crosspost into their own server.</summary>
    Announcement = 5,

    /// <summary>Storefront channel for game developers to sell their game on Discord.</summary>
    Store = 6,

    /// <summary>Temporary sub-channel within an announcement channel.</summary>
    AnnouncementThread = 10,

    /// <summary>Temporary sub-channel within a text channel.</summary>
    Thread = 11,

    /// <summary>Temporary sub-channel within a text channel viewable only by invited (<c>@mentioned</c>) users.</summary>
    PrivateThread = 12,

    /// <summary>A voice channel for hosting events with an audience.</summary>
    Stage = 13
}
