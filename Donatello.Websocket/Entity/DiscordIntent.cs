using System;

namespace Donatello.Websocket.Entity
{

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum DiscordIntent
    {
        /// <summary>
        /// 
        /// </summary>
        All = Unprivileged | GuildMembers | GuildPresences,

        /// <summary>
        /// 
        /// </summary>
        Unprivileged = Guilds | GuildBans | GuildEmojis | GuildIntegrations | GuildWebhooks | GuildInvites | GuildVoiceStates | GuildMessages | GuildMessageReactions | GuildMessageTyping | DirectMessages | DirectMessageReactions | DirectMessageTyping,

        /// <summary>
        /// 
        /// </summary>
        Guilds = 1 << 0,

        /// <summary>
        /// 
        /// </summary>
        GuildMembers = 1 << 1,

        /// <summary>
        /// 
        /// </summary>
        GuildBans = 1 << 2,

        /// <summary>
        /// 
        /// </summary>
        GuildEmojis = 1 << 3,

        /// <summary>
        /// 
        /// </summary>
        GuildIntegrations = 1 << 4,

        /// <summary>
        /// 
        /// </summary>
        GuildWebhooks = 1 << 5,

        /// <summary>
        /// 
        /// </summary>
        GuildInvites = 1 << 6,

        /// <summary>
        /// 
        /// </summary>
        GuildVoiceStates = 1 << 7,

        /// <summary>
        /// 
        /// </summary>
        GuildPresences = 1 << 8,

        /// <summary>
        /// 
        /// </summary>
        GuildMessages = 1 << 9,

        /// <summary>
        /// 
        /// </summary>
        GuildMessageReactions = 1 << 10,

        /// <summary>
        /// 
        /// </summary>
        GuildMessageTyping = 1 << 11,

        /// <summary>
        /// 
        /// </summary>
        DirectMessages = 1 << 12,

        /// <summary>
        /// 
        /// </summary>
        DirectMessageReactions = 1 << 13,

        /// <summary>
        /// 
        /// </summary>
        DirectMessageTyping = 1 << 14
    }
}
