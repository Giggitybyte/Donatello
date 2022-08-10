namespace Donatello.Entity;
using Donatello.Enumeration;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;

/// <summary></summary>
public abstract class DiscordChannel : DiscordEntity, IChannel
{
    internal DiscordChannel(DiscordBot bot, JsonElement json) : base(bot, json)
    {
        var jsonType = json.GetProperty("type").GetInt32();

        if (Enum.IsDefined(typeof(ChannelType), jsonType))
            this.Type = (ChannelType)jsonType;
        else
            throw new JsonException("Unknown channel type.");
    }

    /// <summary>Type of this channel.</summary>
    internal ChannelType Type { get; private init; }
    ChannelType IChannel.Type => this.Type;

    /// <summary>Name of the channel.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary></summary>
    public ReadOnlyCollection<DiscordInvite> Invites { get; }

}
