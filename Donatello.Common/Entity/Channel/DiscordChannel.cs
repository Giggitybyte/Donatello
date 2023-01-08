namespace Donatello.Entity;

using Donatello.Enum;
using Donatello.Extension.Internal;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Abstract implementation of <see cref="IChannel"/>.</summary>
public abstract class DiscordChannel : DiscordEntity, IChannel
{
    internal DiscordChannel(DiscordBot bot, JsonElement json)
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
    public ReadOnlyCollection<DiscordGuildInvite> Invites { get; }

    ChannelType IChannel.Type => this.Type;

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal protected static TChannel Create<TChannel>(JsonElement channelJson, DiscordBot botInstance) where TChannel : DiscordChannel
    {
        var type = channelJson.GetProperty("type").GetInt32();

        DiscordChannel instance = type switch
        {
            0 => new DiscordGuildTextChannel(botInstance, channelJson),
            1 => new DiscordDirectTextChannel(botInstance, channelJson),
            2 => new DiscordGuildVoiceChannel(botInstance, channelJson),
            3 => new DiscordGroupTextChannel(botInstance, channelJson),
            4 => new DiscordCategoryChannel(botInstance, channelJson),
            5 => new DiscordNewsChannel(botInstance, channelJson),
            10 or 11 or 12 => new DiscordThreadChannel(botInstance, channelJson),
            13 => new DiscordStageChannel(botInstance, channelJson),
            14 => new DiscordDirectoryChannel(botInstance, channelJson),
            15 => new DiscordForumChannel(botInstance, channelJson),
            _ => throw new JsonException("Unknown channel type.")
        };

        if (instance is TChannel channel)
            return channel;
        else
            throw new InvalidCastException($"{typeof(TChannel).Name} is an incompatible type parameter for {instance.Type} channel objects.");
    }

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal protected static DiscordChannel Create(JsonElement channelJson, DiscordBot botInstance)
        => Create<DiscordChannel>(channelJson, botInstance);

    /// <summary>Converts a JSON object to the appropriate Discord channel entity and attempts to return it as <typeparamref name="TChannel"/>.</summary>
    internal protected static TChannel Create<TChannel>(JsonObject channelObject, DiscordBot botInstance) where TChannel : DiscordChannel
        => Create<TChannel>(channelObject.AsElement(), botInstance);

    /// <summary>Converts a JSON object to an appropriate Discord channel entity.</summary>
    internal protected static DiscordChannel Create(JsonObject channelJson, DiscordBot botInstance)
        => Create<DiscordChannel>(channelJson, botInstance);
}
