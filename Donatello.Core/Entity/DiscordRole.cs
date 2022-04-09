namespace Donatello.Entity;

using Donatello.Enumeration;
using Donatello.Extension.Internal;
using System;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class DiscordRole : DiscordEntity
{
    private ulong _guildId;

    internal DiscordRole(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary>Role name.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Role color.</summary>
    public Color Color => Color.FromArgb(this.Json.GetProperty("color").GetInt32());

    /// <summary>Whether users with this role are displayed separately from online users in the sidebar.</summary>
    public bool IsPinned => this.Json.GetProperty("hoist").GetBoolean();

    /// <summary>Position of this role in the role hierarchy.</summary>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary>Permission flags.</summary>
    public GuildPermission Permissions
    {
        get
        {
            var property = this.Json.GetProperty("permissions").GetString();
            return (GuildPermission)ulong.Parse(property);
        }
    }

    /// <summary>Whether this role is managed by an <i>integration</i>, e.g. a Discord bot.</summary>
    public bool IsManaged => this.Json.GetProperty("managed").GetBoolean();

    /// <summary>Whether this role can be <c>@mentioned</c>.</summary>
    public bool IsMentionable => this.Json.GetProperty("mentionable").GetBoolean();

    /// <summary>Whether this role is the Nitro booster role.</summary>
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

    /// <summary></summary>
    public ValueTask<DiscordGuild> GetGuildAsync()
        => this.Bot.GetGuildAsync(_guildId);

    /// <summary></summary>
    public async ValueTask<DiscordMember> GetBotAsync()
    {
        if (this.Json.TryGetProperty("tags", out var tagProp))
            if (tagProp.TryGetProperty("bot_id", out var idProp))
            {
                var guild = await GetGuildAsync();
                var member = await guild.GetMemberAsync(idProp.ToUInt64());
            }

        return ValueTask.FromException<DiscordUser>(new InvalidOperationException("Role is not owned by a bot."));
    }

    public ValueTask<DiscordIntegration> GetIntegrationAsync()
    {
        if (this.Json.TryGetProperty("tags", out var tagProp))
            if (tagProp.TryGetProperty("integration_id", out var idProp))
            {
                
            }

        return ValueTask.FromException<DiscordIntegration>(new InvalidOperationException("Role is not owned by an integration."));
    }
}
