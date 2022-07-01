namespace Donatello.Entity;

using System.Drawing;
using System.Text.Json;

/// <summary></summary>
public class DiscordUser : DiscordEntity
{
    public DiscordUser(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    /// <summary>User's global display name.</summary>
    public string Username => this.Json.GetProperty("username").GetString();

    /// <summary>Sequence used to differentiate between users with the same username.</summary>
    public ushort Discriminator => ushort.Parse(this.Json.GetProperty("discriminator").GetString());

    /// <summary>Full Discord tag, e.g. <c>thegiggitybyte#8099</c>.</summary>
    public string Tag => $"{this.Username}#{this.Discriminator}";

    /// <summary>Global user avatar URL.</summary>
    public virtual string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out var prop) && prop.ValueKind is not JsonValueKind.Null)
            {
                var extension = prop.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/{this.Id}/{prop.GetString()}.{extension}";
            }
            else
                return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
        }
    }

    /// <summary>Whether the user is an automated bot user.</summary>
    public bool IsBot => this.Json.TryGetProperty("bot", out var prop) && prop.GetBoolean();

    /// <summary>Whether the user is an urgent message system user.</summary>
    public bool IsSystem => this.Json.TryGetProperty("system", out var prop) && prop.GetBoolean();

    /// <summary>The user's banner color.</summary>
    public Color BannerColor => this.Json.TryGetProperty("accent_color", out var prop) ? Color.FromArgb(prop.GetInt32()) : Color.Empty;

    /// <summary>Returns <see langword="true"/> if the user has chosen a color for their banner, <see langword="false"/> otherwise.</summary>
    /// <param name="bannerColor">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the user's banner color.<br/>
    /// <see langword="false"/> this parameter will be <see cref="Color.Empty"/>.
    /// </param>
    public bool HasBanner(out Color bannerColor)
    {
        if (this.Json.TryGetProperty("accent_color", out var prop) && prop.ValueKind is not JsonValueKind.Null)
            bannerColor = Color.FromArgb(prop.GetInt32());
        else
            bannerColor = Color.Empty;

        return bannerColor != Color.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the user has a banner image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="bannerUrl">
    /// When the method returns:<br/>
    /// <see langword="true"/> this parameter will contain the banner URL,<br/>
    /// <see langword="false"/> this parameter will contain an empty string.
    /// </param>
    public bool HasBanner(out string bannerUrl)
    {
        if (this.Json.TryGetProperty("banner", out var prop) && prop.ValueKind is not JsonValueKind.Null)
            bannerUrl = $"https://cdn.discordapp.com/banners/{this.Id}/{prop.GetString()}";
        else
            bannerUrl = string.Empty;

        return bannerUrl != string.Empty;
    }

    /// <summary>Returns the user's full tag and ID.</summary>
    public override string ToString() => $"{this.Tag} ({this.Id})";
}

