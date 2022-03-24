namespace Donatello.Gateway.Entity;

using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class DiscordMember : DiscordEntity
{
    private readonly ulong _userId;
    private readonly ulong _guildId;

    public DiscordMember(DiscordBot bot, JsonElement json, ulong guildId) : base(bot, json) 
    {
        _userId = this.Json.GetProperty("user").GetProperty("id").AsUInt64();
        _guildId = guildId;
    }

    /// <summary></summary>
    public string Nickname => this.Json.TryGetProperty("nick", out var prop) ? prop.GetString() : string.Empty;

    /// <summary></summary>
    public string AvatarUrl
    {
        get
        {
            if (this.Json.TryGetProperty("avatar", out var prop))
            {
                var avatarHash = prop.GetString();
                if (avatarHash is not null)
                    return $"https://cdn.discordapp.com/guilds/{_guildId}/users/{_userId}/avatars/{prop.GetString()}.png";
            }

            return string.Empty;
        }
    }

    public ValueTask<DiscordUser> GetUserAsync()
        => this.Bot.GetUserAsync(_userId);

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);
}

