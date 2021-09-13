using System.Drawing;
using System.Text.Json;
using Donatello.Interactions.Entities.Enums;

namespace Donatello.Interactions.Entities
{
    /// <summary></summary>
    public sealed class DiscordUser : DiscordEntity
    {
        public DiscordUser(JsonElement json) : base(json) { }

        /// <summary>Additional metadata for this user, e.g. badges.</summary>
        internal UserFlag Flags => this.Json.TryGetProperty("public_flags", out var prop) ? (UserFlag)prop.GetInt32() : UserFlag.None;

        /// <summary>The user's name.</summary>
        public string Username => this.Json.GetProperty("username").GetString();

        /// <summary>Sequence used to differentiate between users with the same username.</summary>
        public ushort Discriminator => ushort.Parse(this.Json.GetProperty("discriminator").GetString());

        /// <summary>Full Discord tag, e.g. <c>thegiggitybyte#8099</c>.</summary>
        public string Tag => $"{this.Username}#{this.Discriminator}";

        /// <summary>User avatar URL.</summary>
        public string AvatarUrl
        {
            get
            {
                var avatarHash = this.Json.GetProperty("avatar").GetString();
                if (!string.IsNullOrEmpty(avatarHash))
                {
                    var extension = avatarHash.StartsWith("a_") ? "gif" : "png";
                    return $"https://cdn.discordapp.com/avatars/{this.Id}/{avatarHash}.{extension}";
                }
                else
                    return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
            }
        }

        /// <summary>User banner URL.</summary>
        /// <remarks>If the user has not uploaded a banner, this property will return <see langword="null"/>.</remarks>
        public string BannerUrl
        {
            get
            {
                if (this.Json.TryGetProperty("banner", out var prop))
                {
                    var bannerHash = prop.GetString();
                    var extension = bannerHash.StartsWith("a_") ? "gif" : "png";

                    return $"https://cdn.discordapp.com/avatars/{this.Id}/{bannerHash}.{extension}";
                }
                else
                    return null;
            }
        }

        /// <summary>Displayed as the user's banner if one has not been uploaded.</summary>
        public Color BannerColor => this.Json.TryGetProperty("accent_color", out var prop) ? Color.FromArgb(prop.GetInt32()) : Color.Empty;

        /// <summary>Whether this user is a bot user.</summary>
        public bool IsBot => this.Json.TryGetProperty("bot", out var prop) && prop.GetBoolean();

        /// <summary>Whether this user is the official Discord system user.</summary>
        public bool IsSystem => this.Json.TryGetProperty("system", out var prop) && prop.GetBoolean();

        /// <summary>Returns the user's full tag and ID.</summary>
        public override string ToString() => $"{this.Tag} ({this.Id})";
    }
}
