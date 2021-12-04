namespace Donatello.Interactions.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Interactions.Entity.Enumeration;
using Donatello.Interactions.Extension;
using Qommon.Collections;

/// <summary>A collection of channels and users.</summary>
public sealed class DiscordGuild : DiscordEntity
{
    public DiscordGuild(DiscordBot bot, JsonElement json) : base(bot, json) { }

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
    public ReadOnlyList<DiscordRole> Roles
    {
        get
        {
            var roles = this.Json.GetProperty("roles").ToEntityArray<DiscordRole>(this.Bot);
            return new ReadOnlyList<DiscordRole>(roles);
        }
    }

    /// <summary></summary>
    public ReadOnlyList<DiscordEmote> Emotes
    {
        get
        {
            var emotes = this.Json.GetProperty("emojis").ToEntityArray<DiscordEmote>(this.Bot);
            return new ReadOnlyList<DiscordEmote>(emotes);
        }
    }

    /// <summary>Custom invite link, e.g. <c>https://discord.gg/wumpus-and-friends</c></summary>
    /// <remarks>May return <see cref="string.Empty"/> if the guild does not have a vanity URL.</remarks>
    public string VanityInviteUrl
    {
        get
        {
            var code = this.Json.GetProperty("vanity_url_code").GetString();

            if (code is not null)
                return $"https://discord.gg/{code}";
            else
                return string.Empty;
        }
    }

    /// <summary>Guild icon URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if an icon has not been uploaded.</remarks>
    public string IconUrl
    {
        get
        {
            var iconHash = this.Json.GetProperty("icon").GetString();

            if (!string.IsNullOrEmpty(iconHash))
            {
                var extension = iconHash.StartsWith("a_") ? "gif" : "png";
                return $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";
            }
            else
                return string.Empty;
        }
    }

    /// <summary>Guild banner URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if the guild does not have a banner.</remarks>
    public string BannerUrl
    {
        get
        {
            var bannerHash = this.Json.GetProperty("banner").GetString();

            if (!string.IsNullOrEmpty(bannerHash))
                return $"https://cdn.discordapp.com/banners/{this.Id}/{bannerHash}.png";
            else
                return string.Empty;
        }
    }

    /// <summary>Splash image URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if a splash image has not been uploaded.</remarks>
    public string InviteSplashUrl
    {
        get
        {
            var splashHash = this.Json.GetProperty("splash").GetString();

            if (!string.IsNullOrEmpty(splashHash))
                return $"https://cdn.discordapp.com/splashes/{this.Id}/{splashHash}.png";
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
