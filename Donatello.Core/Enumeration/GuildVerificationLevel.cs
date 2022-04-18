namespace Donatello.Enumeration;

/// <summary>Guild membership criteria.</summary>
public enum GuildVerificationLevel : ushort
{
    /// <summary>Unrestricted; anyone who joins the guild will be able to interact with it immediately.</summary>
    None,

    /// <summary>Requires new members to have a verified email associated with their Discord account.</summary>
    Low,

    /// <summary>
    /// Requires new members to:<br/>
    /// - Have a verified email associated with their Discord account.<br/>
    /// - Be a registered Discord user for at least 5 minutes.
    /// </summary>
    Medium,

    /// <summary>
    /// Requires new members to:<br/>
    /// - Have a verified email associated with their Discord account.<br/>
    /// - Be a registered Discord user for at least 5 minutes.<br/>
    /// - Be present in the guild for at least 10 minutes.
    /// </summary>
    High,

    /// <summary>
    /// Requires new members to:<br/>
    /// - Have a verified email associated with their Discord account.<br/>
    /// - Be a registered Discord user for at least 5 minutes.<br/>
    /// - Be present in the guild for at least 10 minutes.<br/>
    /// - Have a verified phone number associated with their Discord account.
    /// </summary>
    Highest
}
