namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Enumeration;
using System.Text.Json;

/// <summary></summary>
public abstract class DiscordOverwrite : DiscordEntity
{
    public DiscordOverwrite(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public GuildPermission AllowedPermissions => (GuildPermission)this.Json.GetProperty("allow").GetInt64();

    /// <summary></summary>
    public GuildPermission DeniedPermissions => (GuildPermission)this.Json.GetProperty("deny").GetInt64();
}