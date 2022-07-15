namespace Donatello.Entity;

using System;

public sealed class DiscordInvite
{
    public sealed record InviteMetadata(uint Uses, uint MaxUses, TimeSpan MaxAge, DateTimeOffset CreationDate);

}

