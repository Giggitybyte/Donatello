﻿namespace Donatello.Common.Enum;

/// <summary>Explicit content filter.</summary>
public enum FilterLevel : ushort
{
    /// <summary>Uploaded media will not be scanned.</summary>
    Disabled,

    /// <summary>Uploaded media will only be scanned when sent by a user without any roles.</summary>
    UsersWithoutRoles,

    /// <summary>Uploaded media sent by anyone will be scanned.</summary>
    AllMembers
}
