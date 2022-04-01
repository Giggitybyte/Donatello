namespace Donatello.Entity;

using Donatello.Enumeration;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A collection of channels and users.</summary>
public sealed class DiscordGuild : DiscordEntity
{
    public DiscordGuild(Bot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    internal ReadOnlyList<string> Features => new(this.Json.GetProperty("features").ToStringArray());

    /// <summary></summary>
    internal SystemChannelFlag SystemChannelFlags => (SystemChannelFlag)this.Json.GetProperty("system_channel_flags").GetInt32();

    /// <summary>Guild name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Guild verification level.</summary>
    public GuildVerificationLevel VerificationLevel => (GuildVerificationLevel)this.Json.GetProperty("verification_level").GetInt32();

    /// <summary></summary>
    public GuildContentFilterLevel ContentFilterLevel => (GuildContentFilterLevel)this.Json.GetProperty("explicit_content_filter").GetInt32();

    /// <summary>Amount of time a user must be idle before they are moved to the AFK channel.</summary>
    public TimeSpan AfkTimeout => TimeSpan.FromSeconds(this.Json.GetProperty("afk_timeout").GetInt32());

    /// <summary></summary>
    public int BoostLevel => this.Json.GetProperty("premium_tier").GetInt32();

    /// <summary></summary>
    public ReadOnlyCollection<DiscordRole> Roles => new(this.Json.GetProperty("roles").ToEntityArray<DiscordRole>(this.Bot));

    /// <summary></summary>
    public ReadOnlyList<DiscordEmote> Emotes => new(this.Json.GetProperty("emojis").ToEntityArray<DiscordEmote>(this.Bot));

    /// <summary>Custom invite link, e.g. <c>https://discord.gg/wumpus-and-friends</c></summary>
    /// <remarks>May return <see cref="string.Empty"/> if the guild does not have a vanity URL.</remarks>
    public string VanityInviteUrl => this.Json.TryGetProperty("vanity_url_code", out var prop) ? $"https://discord.gg/{prop.GetString()}" : string.Empty;

    /// <summary>Guild icon URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if an icon has not been uploaded for this guild.</remarks>
    public string IconUrl
    {
        get
        {
            var iconProp = this.Json.GetProperty("icon");

            if (iconProp.ValueKind is not JsonValueKind.Null)
            {
                var iconHash = iconProp.GetString();
                var extension = iconHash.StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";
            }
            else
                return string.Empty;
        }
    }

    /// <summary>Guild banner URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if the guild does not have a banner.</remarks>
    public string BannerUrl => this.Json.TryGetProperty("banner", out var prop) ? $"https://cdn.discordapp.com/banners/{this.Id}/{prop.GetString()}.png" : string.Empty;

    /// <summary>Splash image URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if a splash image has not been uploaded.</remarks>
    public string InviteSplashUrl
    {
        get
        {
            var splashProp = this.Json.GetProperty("splash");

            if (splashProp.ValueKind is not JsonValueKind.Null)
                return $"https://cdn.discordapp.com/splashes/{this.Id}/{splashProp.GetString()}.png";
            else
                return string.Empty;
        }
    }

    /// <summary></summary>
    public Task<DiscordUser> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").AsUInt64());

    /// <summary></summary>
    public Task<DiscordChannel> GetChannelAsync(ulong channelId)
        => this.Bot.GetChannelAsync(channelId);

    /// <summary></summary>
    public Task<DiscordChannel> GetRulesChannelAsync()
        => GetChannelAsync(this.Json.GetProperty("rules_channel_id").AsUInt64());

    /// <summary></summary>
    public Task<DiscordChannel> GetAfkChannelAsync()
        => GetChannelAsync(this.Json.GetProperty("afk_channel_id").AsUInt64());

    /// <summary></summary>
    public Task<DiscordChannel> GetSystemChannelAsync()
        => GetChannelAsync(this.Json.GetProperty("system_channel_id").AsUInt64());
}

