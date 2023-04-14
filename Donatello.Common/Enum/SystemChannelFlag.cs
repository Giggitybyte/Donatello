namespace Donatello.Common.Enum;

using System;

/// <summary>Additional metadata for a guild system channel.</summary>
[Flags]
internal enum SystemChannelFlag : int 
{
    /// <summary>User join messages are disabled.</summary>
    JoinNotificationDisabled = 1 << 0,

    /// <summary>Nitro server boost messages are disabled.</summary>
    BoostNotificationDisabled = 1 << 1,

    /// <summary>Server setup tips are disabled.</summary>
    SetupTipsDisabled = 1 << 2
}
