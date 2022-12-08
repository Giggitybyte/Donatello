namespace Donatello.Entity;

/// <summary>A Discord entity which is managed or created by a <see cref="DiscordBot"/> instance.</summary>
public interface IBotEntity
{
    /// <summary>Bot instance which manages this object.</summary>
    protected DiscordBot Bot { get; }
}
