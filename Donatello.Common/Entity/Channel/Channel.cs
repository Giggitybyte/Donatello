namespace Donatello.Common.Entity.Channel;

using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Enum;
using Extension;
using Guild;
using Guild.Channel;

/// <summary>Abstract implementation of <see cref="IChannel"/>.</summary>
public abstract class Channel : Entity, IChannel
{
    internal Channel(JsonElement json) : base(json)
    {
        var jsonType = json.GetProperty("type").GetUInt16();

        if (Enum.IsDefined(typeof(ChannelType), jsonType))
            this.Type = (ChannelType)jsonType;
        else
            throw new JsonException("Unknown channel type.");
    }

    /// <summary>Type of this channel.</summary>
    protected internal ChannelType Type { get; private init; }

    /// <summary>Name of the channel.</summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary></summary>
    public ReadOnlyCollection<GuildInvite> Invites { get; }

    ChannelType IChannel.Type => this.Type;
}
