namespace Donatello.Entity;

using System;

public class DiscordGuildInvite : DiscordEntity, IGuildEntity
{
    public record InviteMetadata(uint Uses, uint MaxUses, TimeSpan MaxAge, DateTimeOffset CreationDate);

}

