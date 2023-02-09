namespace Donatello.Entity;

using Enum;
using Extension.Internal;
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

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    protected internal static TChannel Create<TChannel>(Bot botInstance, JsonElement channelJson) where TChannel : class, IChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        Channel instance = type switch
        {
            0 => new GuildTextChannel(botInstance, channelJson),
            1 => new DirectMessageChannel(botInstance, channelJson),
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
    protected internal static Channel Create(Bot botInstance, JsonElement channelJson)
        => Create<Channel>(botInstance, channelJson);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    protected internal static TChannel Create<TChannel>(Bot botInstance, JsonObject channelObject) where TChannel : class, IChannel
        => Create<TChannel>(botInstance, channelObject.AsElement());

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    protected internal static Channel Create(Bot botInstance, JsonObject channelJson)
        => Create<Channel>(botInstance, channelJson);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    protected internal static TChannel Create<TChannel>(Bot botInstance, JsonElement channelJson, Snowflake guildId) where TChannel : class, IGuildChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        Channel instance = type switch
        {
            0 => new GuildTextChannel(botInstance, channelJson, guildId),
            1 or 3 => throw new InvalidOperationException("DM channels cannot be deserialized with this overload."),
            2 => new GuildVoiceChannel(botInstance, channelJson, guildId),
            4 => new GuildCategoryChannel(botInstance, channelJson, guildId),
            5 => new GuildNewsChannel(botInstance, channelJson, guildId),
            10 or 11 or 12 => new GuildThreadChannel(botInstance, channelJson, guildId),
            13 => new GuildStageChannel(botInstance, channelJson, guildId),
            14 => new HubDirectoryChannel(botInstance, channelJson, guildId),
            15 => new GuildForumChannel(botInstance, channelJson, guildId),
            _ => throw new JsonException("Unknown channel type.")
        };

        if (instance is TChannel channel)
            return channel;
        else
            throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    protected internal static TChannel Create<TChannel>(Bot botInstance, JsonObject channelObject, Snowflake guildId) where TChannel : class, IGuildChannel
        => Create<TChannel>(botInstance, channelObject.AsElement(), guildId);
}
