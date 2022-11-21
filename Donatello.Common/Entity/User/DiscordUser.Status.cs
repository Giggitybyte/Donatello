namespace Donatello.Entity.User;

using Donatello.Enum;

public partial class DiscordUser
{
    /// <summary></summary>
    public struct Status
    {
        /// <summary></summary>
        public UserStatus Desktop { get; internal set; }

        /// <summary></summary>
        public UserStatus Mobile { get; internal set; }

        /// <summary></summary>
        public UserStatus Web { get; internal set; }
    }
}
