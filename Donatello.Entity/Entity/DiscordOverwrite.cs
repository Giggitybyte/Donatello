namespace Donatello.Core.Entity;

using System.Text.Json;

/// <summary></summary>
public abstract class DiscordOverwrite : DiscordEntity
{
    public DiscordOverwrite(AbstractBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public GuildPermission AllowedPermissions => (GuildPermission)this.Json.GetProperty("allow").GetInt64();

    /// <summary></summary>
    public GuildPermission DeniedPermissions => (GuildPermission)this.Json.GetProperty("deny").GetInt64();
}