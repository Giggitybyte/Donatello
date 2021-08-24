using System.Drawing;
using System.Text.Json;
using Donatello.Interactions.Enums;

namespace Donatello.Interactions.Entities
{
    /// <summary></summary>
    public class DiscordUser
    {
        private readonly JsonElement _json;

        internal DiscordUser(JsonElement json)
        {
            _json = json;
        }

        /// <summary>Unique Discord user ID</summary>
        public ulong Id => ulong.Parse(_json.GetProperty("id").GetString());

        /// <summary>The user's name</summary>
        public string Username => _json.GetProperty("username").GetString();

        /// <summary>Numeric sequence used to differentiate between users with the same username</summary>
        public ushort Discriminator => ushort.Parse(_json.GetProperty("discriminator").GetString());

        /// <summary>Full Discord tag, e.g. <c>thegiggitybyte#8099</c></summary>
        public string Tag => $"{this.Username}#{this.Discriminator}";

        /// <summary></summary>
        public DiscordFlag? Flags
        {
            get
            {
                if (_json.TryGetProperty("public_flags", out var prop))
                    return (DiscordFlag)prop.GetInt32();
                else
                    return null;
            }
        }

        /// <summary>Direct URL for the user's avatar</summary>
        public string AvatarUrl
        {
            get
            {
                var avatarHash = _json.GetProperty("avatar").GetString();
                var isAnimated = avatarHash.StartsWith("a_");

                if (!string.IsNullOrEmpty(avatarHash))
                    return $"https://cdn.discordapp.com/avatars/{this.Id}/{avatarHash}.{(isAnimated ? "gif" : "png")}";
                else
                    return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
            }
        }

        /// <summary>Direct URL for the user's banner</summary>
        public string BannerUrl
        {
            get
            {
                if (_json.TryGetProperty("banner", out var prop))
                {
                    var bannerHash = prop.GetString();
                    var isAnimated = bannerHash.StartsWith("a_");

                    return $"https://cdn.discordapp.com/avatars/{this.Id}/{bannerHash}.{(isAnimated ? "gif" : "png")}";
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// A solid color derived from colors of the user's avatar. 
        /// Used as the default banner if the user has not uploaded one.
        /// </summary>
        public Color? BannerColor
        {
            get
            {
                if (_json.TryGetProperty("accent_color", out var prop))
                    return Color.FromArgb(prop.GetInt32());
                else
                    return null;
            }
        }

        /// <summary>Whether or not this user is a bot user</summary>
        public bool? IsBot
        {
            get
            {
                if (_json.TryGetProperty("bot", out var prop))
                    return prop.GetBoolean();
                else
                    return null;
            }
        }

        /// <summary>Whether or not this user is the official Discord system user</summary>
        public bool? IsSystem
        {
            get
            {
                if (_json.TryGetProperty("system", out var prop))
                    return prop.GetBoolean();
                else
                    return null;
            }
        }

        /// <summary>Returns the tag for this user</summary>
        public override string ToString()
            => this.Tag;
    }
}
