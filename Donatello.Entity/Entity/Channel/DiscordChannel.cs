namespace Donatello.Core.Entity;

using Donatello.Core.Enumeration;
using System;
using System.Text.Json;

/// <summary></summary>
public abstract class DiscordChannel : DiscordEntity
{
    internal DiscordChannel(AbstractBot bot, JsonElement json) : base(bot, json)
    {
        var jsonType = json.GetProperty("type").GetInt32();
        if (Enum.IsDefined(typeof(ChannelType), jsonType))
            this.Type = (ChannelType)jsonType;
        else
            throw new JsonException("Unknown channel type.");
    }

    /// <summary>Type of this channel.</summary>
    internal ChannelType Type { get; private init; }

    /// <summary>Name of the channel.</summary>
    public string Name => this.Json.GetProperty("name").GetString();
}
