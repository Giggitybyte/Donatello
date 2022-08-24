﻿namespace Donatello.Extension.User;

using Donatello.Entity;
using System.Threading.Tasks;

public static class UserExtensionMethods
{
    public static Task<DiscordGuildMember> GetMemberAsync(this DiscordUser user, DiscordGuild guild)
        => guild.GetMemberAsync(user);
}

