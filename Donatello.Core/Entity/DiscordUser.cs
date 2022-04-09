namespace Donatello.Entity;

using Donatello.Enumeration;
using System.Drawing;
using System.Text.Json;

/// <summary></summary>
public class DiscordUser : DiscordEntity
{
    public DiscordUser(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Additional metadata for this user, e.g. badges.</summary>
    internal UserFlag Flags => this.Json.TryGetProperty("public_flags", out var property) ? (UserFlag)property.GetInt32() : UserFlag.None;

    /// <summary>The user's global display name.</summary>
    public string Username => this.Json.GetProperty("username").GetString();

    /// <summary>Sequence used to differentiate between users with the same username.</summary>
    public ushort Discriminator => ushort.Parse(this.Json.GetProperty("discriminator").GetString());

    /// <summary>Full Discord tag, e.g. <c>thegiggitybyte#8099</c>.</summary>
    public string Tag => $"{this.Username}#{this.Discriminator}";

    /// <summary>Global user avatar URL.</summary>
    public string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out var avatarHash) && avatarHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = avatarHash.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/{this.Id}/{avatarHash.GetString()}.{extension}";
            }
            else
                return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
        }
    }

    /// <summary>Whether this user is a bot account.</summary>
    public bool IsBot => this.Json.TryGetProperty("bot", out var property) && property.GetBoolean();

    /// <summary>Whether this user is the official Discord system account.</summary>
    public bool IsSystem => this.Json.TryGetProperty("system", out var property) && property.GetBoolean();

    /// <summary>User banner URL.</summary>
    /// <remarks>Will return <see cref="string.Empty"/> if the user has not uploaded a banner image.</remarks>
    public string BannerUrl
    {
        get
        {
            if (this.Json.TryGetProperty("banner", out var bannerHash) && bannerHash.ValueKind is not JsonValueKind.Null)
            {
                var extension = bannerHash.GetString().StartsWith("a_") ? "gif" : "png";

                return $"https://cdn.discordapp.com/avatars/{this.Id}/{bannerHash.GetString()}.{extension}";
            }
            else
                return string.Empty;
        }
    }

    /// <summary>Displayed as the user's banner if the user has not uploaded a banner image.</summary>
    public Color BannerColor => this.Json.TryGetProperty("accent_color", out var property) ? Color.FromArgb(property.GetInt32()) : Color.Empty;

    /// <summary>The type of Nitro subscription on the user's account.</summary>
    public NitroType Nitro => this.Json.TryGetProperty("premium_type", out var property) ? (NitroType)property.GetUInt16() : NitroType.None;

    /// <summary>Returns the user's full tag and ID.</summary>
    public override string ToString() => $"{this.Tag} ({this.Id})";

}
