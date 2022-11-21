namespace Donatello.Entity;

/// <summary>An object which is managed by a <see cref="DiscordBot"/> instance.</summary>
public interface IBotEntity
{
    /// <summary>Bot instance which contains and manages this object.</summary>
    protected DiscordBot Bot { get; }
}
