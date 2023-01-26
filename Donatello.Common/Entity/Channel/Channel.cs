namespace Donatello.Entity;

using Donatello.Enum;
using Donatello.Extension.Internal;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Abstract implementation of <see cref="IChannel"/>.</summary>
public abstract class Channel : Entity, IChannel
{
    internal Channel(Bot bot, JsonElement json)
        : base(bot, json)
    {
        var jsonType = json.GetProperty("type").GetInt32();

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

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal protected static TChannel Create<TChannel>(JsonElement channelJson, Bot botInstance) where TChannel : class, IChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        Channel instance = type switch
        {
            0 => new GuildTextChannel(botInstance, channelJson),
            1 => new DirectTextChannel(botInstance, channelJson),
            2 => new GuildVoiceChannel(botInstance, channelJson),
            3 => throw new ArgumentException("Group DMs are not supported.", nameof(channelJson)),
            4 => new GuildCategoryChannel(botInstance, channelJson),
            5 => new GuildNewsChannel(botInstance, channelJson),
            10 or 11 or 12 => new GuildThreadChannel(botInstance, channelJson),
            13 => new GuildStageChannel(botInstance, channelJson),
            14 => new HubDirectoryChannel(botInstance, channelJson),
            15 => new GuildForumChannel(botInstance, channelJson),
            _ => throw new JsonException("Unknown channel type.")
        };

        if (instance is TChannel channel)
            return channel;
        else
            throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal protected static Channel Create(JsonElement channelJson, Bot botInstance)
        => Create<Channel>(channelJson, botInstance);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal protected static TChannel Create<TChannel>(JsonObject channelObject, Bot botInstance) where TChannel : class, IChannel
        => Create<TChannel>(channelObject.AsElement(), botInstance);

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal protected static Channel Create(JsonObject channelJson, Bot botInstance)
        => Create<Channel>(channelJson, botInstance);
}
