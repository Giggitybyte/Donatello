namespace Donatello.Interactions.Entities.Enums;

using System;

/// <summary>Additional user metadata.</summary>
[Flags]
public enum UserFlag
{
    /// <summary>User does not have any flags.</summary>
    None = 0,

    /// <summary>User is a Discord employee.</summary>
    Employee = 1 << 0,

    /// <summary>User is member of the Discord partner program.</summary>
    Partner = 1 << 1,

    /// <summary>User was an attendee to a HypeSquad event.</summary>
    HypeSquadEvent = 1 << 2,

    /// <summary>User is a tier 1 Discord tester.</summary>
    BugHunter = 1 << 3,

    /// <summary>User is a member of HypeSquad house Bravery.</summary>
    HouseBravery = 1 << 6,

    /// <summary>User is a member of HypeSquad house Brilliance.</summary>
    HouseBrilliance = 1 << 7,

    /// <summary>User is a member of HypeSquad house Balance.</summary>
    HouseBalance = 1 << 8,

    /// <summary>User was a Legacy Nitro subscriber.</summary>
    EarlySupporter = 1 << 9,

    /// <summary>The user is the official Discord system user.</summary>
    System = 1 << 12,

    /// <summary>User is a tier 2 Discord tester.</summary>
    BugSquasher = 1 << 14,

    /// <summary>The user is a good bot.</summary>
    VerifiedBot = 1 << 16,

    /// <summary>User submitted at least 1 bot to Discord for verification before August 20, 2020.</summary>
    EarlyVerifiedBotDev = 1 << 17,

    /// <summary>User is a certified Discord moderator.</summary>
    Moderator = 1 << 18
}
