namespace Donatello.Common.Entity;

/// <summary>A Discord entity which is managed or created by a <see cref="Common.Bot"/> instance.</summary>
public interface IBotEntity
{
    /// <summary>Bot instance which manages this object.</summary>
    protected Bot Bot { get; }
}
