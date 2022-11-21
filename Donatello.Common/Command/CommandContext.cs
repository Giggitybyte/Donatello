namespace Donatello.Command;

using Donatello.Entity;

public abstract class CommandContext
{
    public DiscordBot Bot { get; set; }

    public DiscordUser User { get; set; }

    public DiscordChannel Channel { get; set; }
}
