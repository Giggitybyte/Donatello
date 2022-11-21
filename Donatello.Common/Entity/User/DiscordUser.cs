namespace Donatello.Entity;

using Donatello.Enum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json;

/// <summary></summary>
public partial class DiscordUser : DiscordEntity
{
    public DiscordUser(DiscordBot bot, JsonElement jsonElement)
        : base(bot, jsonElement)
    {

    }

    /// <summary></summary>
    internal Nullable<JsonElement> Presense { get; set; }

    /// <summary>User's global display name.</summary>
    public string Username => this.Json.GetProperty("username").GetString();

    /// <summary>Numeric sequence used to differentiate between users with the same username.</summary>
    public ushort Discriminator => ushort.Parse(this.Json.GetProperty("discriminator").GetString());

    /// <summary>Full Discord tag, e.g. <c>thegiggitybyte#8099</c>.</summary>
    public string Tag => $"{this.Username}#{this.Discriminator}";

    /// <summary>Global user avatar URL.</summary>
    public virtual string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out JsonElement prop) && prop.ValueKind is not JsonValueKind.Null)
            {
                var extension = prop.GetString().StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/avatars/{this.Id}/{prop.GetString()}.{extension}";
            }
            else
                return $"https://cdn.discordapp.com/embed/avatars/{this.Discriminator % 5}.png";
        }
    }

    /// <summary>Whether the user is an automated bot user.</summary>
    public bool BotUser => this.Json.TryGetProperty("bot", out JsonElement prop) && prop.GetBoolean();

    /// <summary>Whether the user is an urgent message system user.</summary>
    public bool SystemUser => this.Json.TryGetProperty("system", out JsonElement prop) && prop.GetBoolean();

    /// <summary>The user's banner color.</summary>
    public Color BannerColor => this.Json.TryGetProperty("accent_color", out JsonElement prop) ? Color.FromArgb(prop.GetInt32()) : Color.Empty;

    /// <summary></summary>
    public bool HasStatus(out UserStatus desktop, out UserStatus mobile, out UserStatus web)
    {
        desktop = UserStatus.Offline;
        mobile = UserStatus.Offline;
        web = UserStatus.Offline;

        if (this.Presense.HasValue)
        {
            if (this.Presense.Value.TryGetProperty("client_status", out JsonElement clientStatus))
            {
                if (clientStatus.TryGetProperty("desktop", out JsonElement desktopStatus))
                    desktop = Enum.Parse<UserStatus>(desktopStatus.GetString());

                if (clientStatus.TryGetProperty("mobile", out JsonElement mobileStatus))
                    mobile = Enum.Parse<UserStatus>(mobileStatus.GetString());

                if (clientStatus.TryGetProperty("web", out JsonElement webStatus))
                    web = Enum.Parse<UserStatus>(webStatus.GetString());

                return true;
            }
            else if (this.Presense.Value.TryGetProperty("status", out JsonElement prop))
            {
                var status = Enum.Parse<UserStatus>(prop.GetString());

                desktop = status;
                mobile = status;
                web = status;

                return true;
            }
        }

        return false;
    }

    /// <summary></summary>
    public bool HasActivities(out ReadOnlyCollection<Activity> activities)
    {
        var userActivities = new List<Activity>();

        if (this.Presense.HasValue && this.Presense.Value.TryGetProperty("activities", out JsonElement activityArray))
        {
            foreach (var activity in activityArray.EnumerateArray())
                userActivities.Add(new Activity() { })
        }
        else
        {
            
        }
        activities = userActivities.AsReadOnly();
        return activities.Count > 0;
    }

    /// <summary>Returns <see langword="true"/> if the user has a banner image uploaded, <see langword="false"/> otherwise.</summary>
    /// <param name="bannerUrl">When the method returns <see langword="true"/> this parameter will contain the banner URL; otherwise it'll contain <see cref="string.Empty"/>.</param>
    public bool HasBannerImage(out string bannerUrl)
    {
        bannerUrl = this.Json.TryGetProperty("banner", out JsonElement prop) && prop.ValueKind is not JsonValueKind.Null
            ? $"https://cdn.discordapp.com/banners/{this.Id}/{prop.GetString()}"
            : string.Empty;

        return bannerUrl != string.Empty;
    }

    /// <summary>Returns <see langword="true"/> if the user has any public flags on their account.</summary>
    /// <param name="flags">When the method returns <see langword="true"/>, this parameter will contain the flags for the user; otherwise it'll contain <see cref="Flag.None"/>.</param>
    public bool HasFlags(out UserFlag flags)
    {
        flags = this.Json.TryGetProperty("public_flags", out JsonElement prop)
            ? (UserFlag)prop.GetInt32()
            : UserFlag.None;

        return flags != UserFlag.None;
    }

    /// <summary>Returns the user's full tag and ID.</summary>
    public override string ToString() => $"{this.Tag} ({this.Id})";
}

