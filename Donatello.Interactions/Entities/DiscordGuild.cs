using System;
using System.Text.Json;
using System.Threading.Tasks;
using Donatello.Interactions.Entities.Enums;
using Donatello.Interactions.Extensions;
using Qommon.Collections;

namespace Donatello.Interactions.Entities
{
    /// <summary>A collection of channels and users.</summary>
    public sealed class DiscordGuild : DiscordEntity
    {
        public DiscordGuild(JsonElement json) : base(json) { }

        /// <summary></summary>
        internal ReadOnlyList<string> Features
            => new(Json.GetProperty("features").ToStringArray());

        /// <summary></summary>
        internal SystemChannelFlag SystemChannelFlags
            => (SystemChannelFlag)Json.GetProperty("system_channel_flags").GetInt32();

        /// <summary>Guild name.</summary>
        public string Name
            => Json.GetProperty("name").GetString();

        /// <summary>Guild verification level.</summary>
        public GuildVerificationLevel VerificationLevel
            => (GuildVerificationLevel)Json.GetProperty("verification_level").GetInt32();

        /// <summary></summary>
        public GuildContentFilterLevel ContentFilterLevel
            => (GuildContentFilterLevel)Json.GetProperty("explicit_content_filter").GetInt32();

        /// <summary>Amount of time a user must be idle before they are moved to the AFK channel.</summary>
        public TimeSpan AfkTimeout
            => TimeSpan.FromSeconds(Json.GetProperty("afk_timeout").GetInt32());

        /// <summary></summary>
        public int BoostLevel
            => Json.GetProperty("premium_tier").GetInt32();

        /// <summary></summary>
        public ReadOnlyList<DiscordRole> Roles
            => new(Json.GetProperty("roles").ToEntityArray<DiscordRole>());

        /// <summary></summary>
        public ReadOnlyList<DiscordEmote> Emotes
            => new(Json.GetProperty("emojis").ToEntityArray<DiscordEmote>());

        public ReadOnlyList<>

        /// <summary>Custom invite link, e.g. <c>https://discord.gg/wumpus-and-friends</c></summary>
        /// <remarks>May return <see langword="null"/> if the guild does not have a vanity URL.</remarks>
        public string VanityInviteUrl
        {
            get
            {
                var code = Json.GetProperty("vanity_url_code").GetString();

                if (code is not null)
                    return $"https://discord.gg/{code}";
                else
                    return null;
            }
        }

        /// <summary>Guild icon URL.</summary>
        /// <remarks>May return <see langword="null"/> if an icon has not been uploaded.</remarks>
        public string IconUrl
        {
            get
            {
                var iconHash = Json.GetProperty("icon").GetString();

                if (!string.IsNullOrEmpty(iconHash))
                {
                    var extension = iconHash.StartsWith("a_") ? "gif" : "png";
                    return $"https://cdn.discordapp.com/icons/{this.Id}/{iconHash}.{extension}";
                }
                else
                    return null;
            }
        }

        /// <summary>Guild banner URL.</summary>
        /// <remarks>May return <see langword="null"/> if the guild does not have a banner.</remarks>
        public string BannerUrl
        {
            get
            {
                var bannerHash = Json.GetProperty("icon").GetString();

                if (!string.IsNullOrEmpty(bannerHash))
                    return $"https://cdn.discordapp.com/banners/{this.Id}/{bannerHash}.png";
                else
                    return null;
            }
        }

        /// <summary>Splash image URL.</summary>
        /// <remarks>May return <see langword="null"/> if a splash image has not been uploaded.</remarks>
        public string InviteSplashUrl
        {
            get
            {
                var splashHash = Json.GetProperty("splash").GetString();

                if (!string.IsNullOrEmpty(splashHash))
                    return $"https://cdn.discordapp.com/splashes/{this.Id}/{splashHash}.png";
                else
                    return null;
            }
        }

        public async ValueTask<DiscordUser> GetOwnerAsync()
        {
            var id = Json.GetProperty("owner_id").GetString();
        }

        public async ValueTask<DiscordChannel> GetRulesChannelAsync()
        {
            var id = Json.GetProperty("rules_channel_id").GetString();
        }

        /// <summary></summary>
        public async ValueTask<DiscordChannel> GetAfkChannelAsync()
        {
            var id = Json.GetProperty("afk_channel_id").GetString();
        }

        /// <summary></summary>
        public async ValueTask<DiscordChannel> GetSystemChannelAsync()
        {
            var id = Json.GetProperty("system_channel_id").GetString();
        }
    }
}
