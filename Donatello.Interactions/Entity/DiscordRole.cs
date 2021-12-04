namespace Donatello.Interactions.Entity;

using System.Text.Json;
using Donatello.Interactions.Entity.Enumeration;

using RoleColor = System.Drawing.Color;

public sealed class DiscordRole : DiscordEntity
{
    internal DiscordRole(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Role name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Role color.</summary>
    public RoleColor Color => RoleColor.FromArgb(this.Json.GetProperty("color").GetInt32());

    /// <summary>Position of this role in the role hierarchy.</summary>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary>Permission flags.</summary>
    public GuildPermission Permissions
    {
        get
        {
            var prop = this.Json.GetProperty("permissions").GetString();
            var permissions = long.Parse(prop);

            return (GuildPermission)permissions;
        }
    }

    /// <summary>Whether users with this role are displayed separately from online users in the sidebar.</summary>
    public bool IsHoisted => this.Json.GetProperty("hoist").GetBoolean();

    /// <summary>Whether this role is managed by an <i>integration</i>, e.g. a Discord bot.</summary>
    public bool IsManaged => this.Json.GetProperty("managed").GetBoolean();

    /// <summary>Whether this role can be <c>@mentioned</c>.</summary>
    public bool IsMentionable => this.Json.GetProperty("mentionable").GetBoolean();

    /// <summary>Whether this role is the Nitro Booster role.</summary>
    public bool IsBoostRole
    {
        get
        {
            if (this.Json.TryGetProperty("tags", out var tagProp))
                if (tagProp.TryGetProperty("premium_subscriber", out var boosterProp))
                    return boosterProp.GetBoolean();

            return false;
        }
    }
}
