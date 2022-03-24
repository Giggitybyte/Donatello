namespace Donatello.Gateway.Entity;

using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public sealed class DiscordGuild : DiscordEntity
{
    private MemoryCache _memberCache;

    public DiscordGuild(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        _memberCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary></summary>
    public string Name { get => this.Json.GetProperty("name").GetString(); }

    /// <summary>Guild icon URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if an icon has not been uploaded for this guild.</remarks>
    public string IconUrl
    {
        get
        {
            var iconHash = this.Json.GetProperty("icon").GetString();

            if (string.IsNullOrEmpty(iconHash) is false)
            {
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

    public DiscordUs
}

