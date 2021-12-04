namespace Donatello.Interactions.Command;

using System;
using Donatello.Interactions.Entity;
using Qmmands;

/// <summary>
/// 
/// </summary>
public sealed class DiscordCommandContext : CommandContext
{
    private readonly string _responseToken;
    private readonly DateTime _tokenExpirationDate;

    internal DiscordCommandContext(string responseToken, IServiceProvider serviceProvider = null) : base(serviceProvider)
    {
        _responseToken = responseToken;
        _tokenExpirationDate = DateTime.Now + TimeSpan.FromMinutes(15);
    }

    /// <summary></summary>
    public DiscordUser User { get; internal set; }

    /// <summary></summary>
    public DiscordGuild Guild { get; internal set; }

    /// <summary></summary>
    public DiscordChannel Channel { get; internal set; }
}
