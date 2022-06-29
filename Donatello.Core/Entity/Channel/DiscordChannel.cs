namespace Donatello.Entity;

using Donatello.Enumeration;
using System.Text.Json;

/// <summary>A guild or DM channel within Discord.</summary>
public abstract class DiscordChannel : DiscordEntity
{
    internal DiscordChannel(DiscordApiBot bot, JsonElement jsonObject) : base(bot, jsonObject) { }

    /// <summary>The type of this channel.</summary>
    public ChannelType Type => (ChannelType)this.Json.GetProperty("type").GetInt16();

    /// <summary>The name of this channel.</summary>
    public string Name => this.Json.TryGetProperty("name", out var prop) ? prop.GetString() : string.Empty;
}

