﻿namespace Donatello.Interactions.Entity;

using Donatello.Interactions.Entity.Enumeration;
using System.Text.Json;

/// <summary></summary>
public class DiscordOverwrite : DiscordEntity
{
    public DiscordOverwrite(DiscordBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public GuildPermission AllowedPermissions => (GuildPermission)this.Json.GetProperty("allow").GetInt64();

    /// <summary></summary>
    public GuildPermission DeniedPermissions => (GuildPermission)this.Json.GetProperty("deny").GetInt64();
}