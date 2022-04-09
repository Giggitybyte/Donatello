namespace Donatello.Entity;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Custom guild emote.</summary>
public sealed class DiscordEmoji : DiscordEntity
{
    private ulong _guildId;

    public DiscordEmoji(DiscordApiBot bot, JsonElement json, ulong guildId) : base(bot, json) 
    { 
        _guildId = guildId;
    }

    /// <summary>Emote name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary></summary>
    public ValueTask<EntityCollection<DiscordRole>> GetAllowedRolesAsync()
    {
        if (this.Json.TryGetProperty("roles", out var property))
        {
            var roles = new Dictionary<ulong, DiscordRole>();

            foreach (var roleId in property)


        }
        else
            return ValueTask.FromResult(EntityCollection<DiscordRole>.Empty);
    }
}
