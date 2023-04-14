namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Entity.Channel;
using Enum;
using Extension;

/// <summary>A channel associated with a guild.</summary>
public abstract class GuildChannel : GuildEntity, IChannel
{
    protected GuildChannel(JsonElement entityJson, Bot bot) : base(entityJson, bot) { }
    protected GuildChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }

    /// <inheritdoc cref="IChannel.Type"/>
    public ChannelType Type => (ChannelType)this.Json.GetProperty("type").GetInt32();
    
    /// <inheritdoc cref="IChannel.Name"/>
    public string Name { get; }

    /// <inheritdoc cref="IGuildChannel.Position"/>
    public int Position => this.Json.GetProperty("position").GetInt32();

    /// <summary></summary>
    public bool Nsfw => throw new NotImplementedException();

    /// <summary></summary>
    public bool HasParent(out GuildChannel parent)
        => throw new NotImplementedException();
}