﻿using System.Text.Json;
using Donatello.Interactions.Entities.Enums;

using RoleColor = System.Drawing.Color;

namespace Donatello.Interactions.Entities
{
    public sealed class DiscordRole : DiscordEntity
    {
        internal DiscordRole(JsonElement json) : base(json) { }

        /// <summary>Role name.</summary>
        public string Name => Json.GetProperty("name").GetString();

        /// <summary>Role color.</summary>
        public RoleColor Color => RoleColor.FromArgb(Json.GetProperty("color").GetInt32());

        /// <summary>Position of this role in the role hierarchy.</summary>
        public int Position => Json.GetProperty("position").GetInt32();

        /// <summary>Permission flags.</summary>
        public GuildPermission Permissions
        {
            get
            {
                var prop = Json.GetProperty("permissions").GetString();
                var permissions = long.Parse(prop);

                return (GuildPermission)permissions;
            }
        }

        /// <summary>Whether users with this role are displayed separately from online users in the sidebar.</summary>
        public bool IsHoisted => Json.GetProperty("hoist").GetBoolean();

        /// <summary>Whether this role is managed by an <i>integration</i>, e.g. a Discord bot.</summary>
        public bool IsManaged => Json.GetProperty("managed").GetBoolean();

        /// <summary>Whether this role can be <c>@mentioned</c>.</summary>
        public bool IsMentionable => Json.GetProperty("mentionable").GetBoolean();

        /// <summary>Whether this role is the Nitro Booster role.</summary>
        public bool IsBoosterRole
        {
            get
            {
                if (Json.TryGetProperty("tags", out var tagProp))
                    if (tagProp.TryGetProperty("premium_subscriber", out var boosterProp))
                        return boosterProp.GetBoolean();

                return false;
            }
        }
    }
}
