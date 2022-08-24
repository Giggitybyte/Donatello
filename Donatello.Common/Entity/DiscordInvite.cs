namespace Donatello.Entity;

using System;

public class DiscordInvite
{
    public record InviteMetadata(uint Uses, uint MaxUses, TimeSpan MaxAge, DateTimeOffset CreationDate);

}

