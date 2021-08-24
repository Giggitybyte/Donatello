using System;

namespace Donatello.Interactions.Enums
{
    /// <summary>Additional user metadata</summary>
    [Flags]
    public enum DiscordFlag
    {
        /// <summary>Default value</summary>
        None = 0,

        /// <summary>Discord employee</summary>
        Employee = 1 << 0,

        /// <summary>Member of the Discord partner program</summary>
        Partner = 1 << 1,

        /// <summary>Organizer and/or attendee to a HypeSquad event</summary>
        HypeSquadEvents = 1 << 2,

        /// <summary>Tier 1 Discord tester</summary>
        BugHunter = 1 << 3,

        /// <summary>HypeSquad house Bravery</summary>
        HouseBravery = 1 << 6,

        /// <summary>HypeSquad house Brilliance</summary>
        HouseBrilliance = 1 << 7,

        /// <summary>HypeSquad house Balance</summary>
        HouseBalance = 1 << 8,

        /// <summary>Legacy Nitro subscriber</summary>
        EarlySupporter = 1 << 9,

        /// <summary>Official Discord system user</summary>
        System = 1 << 12,

        /// <summary>Tier 2 Discord tester</summary>
        BugSquasher = 1 << 14,

        /// <summary>Beep boop</summary>
        VerifiedBot = 1 << 16,

        /// <summary>Submitted at least 1 bot to Discord for verification before August 20, 2020</summary>
        EarlyBotDev = 1 << 17,

        /// <summary>Certified Discord moderator</summary>
        Moderator = 1 << 18
    }
}
