namespace Donatello.Gateway.Entity;

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class DiscordGuild : DiscordEntity
{
    private MemoryCache _memberCache, _voiceStateCache;

    public DiscordGuild(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        _memberCache = new MemoryCache(new MemoryCacheOptions());
        _voiceStateCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary></summary>
    public string Name => this.Json.GetProperty("name").GetString();

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

    /// <summary>Amount of time a user must be idle before they are moved to the AFK channel.</summary>
    public TimeSpan AfkTimeout => TimeSpan.FromSeconds(this.Json.GetProperty("afk_timeout").GetInt32());



    /// <summary>Fetches the user who owns the guild.</summary>
    public ValueTask<DiscordUser> GetOwnerAsync()
        => this.Bot.GetUserAsync(this.Json.GetProperty("owner_id").AsUInt64());

    /// <summary></summary>
    public ValueTask<DiscordVoiceChannel> GetAfkChannelAsync()
        => this.Bot.GetChannelAsync<DiscordVoiceChannel>(this.Json.GetProperty("afk_channel_id").AsUInt64());
}

