namespace Donatello.Common.Entity.Voice;

using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Extension;
using User;

/// <summary>Representation of a user's connection to a voice channel.</summary>
public sealed class DiscordVoiceState : IJsonEntity, IBotEntity
{
    public DiscordVoiceState(Bot bot, JsonElement entityJson)
    {
        this.Json = entityJson;
        this.Bot = bot;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    internal JsonElement Json { get; private set; }

    /// <inheritdoc cref="IBotEntity.Bot"/>
    internal Bot Bot { get; private set; }

    public Snowflake UserId => this.Json.GetProperty("user_id").ToSnowflake();

    /// <summary>Voice channel session ID.</summary>
    public string SessionId => this.Json.GetProperty("session_id").GetString();

    /// <summary>Whether the user has been deafened by a server moderator.</summary>
    public bool Deafened => this.Json.GetProperty("deaf").GetBoolean();

    /// <summary>Whether the user has deafened themself.</summary>
    public bool SelfDeafened => this.Json.GetProperty("self_deaf").GetBoolean();

    /// <summary>Whether the user has been muted by a server moderator.</summary>
    public bool Muted => this.Json.GetProperty("mute").GetBoolean();

    /// <summary>Whether the user has muted themself.</summary>
    public bool SelfMuted => this.Json.GetProperty("self_mute").GetBoolean();

    /// <summary>Whether the user is live streaming video from an app, game, their desktop.</summary>
    public bool Streaming => this.Json.GetProperty("self_stream").GetBoolean();

    /// <summary>Whether the user is live streaming video from a camera.</summary>
    public bool Camera => this.Json.GetProperty("self_video").GetBoolean();

    /// <summary>Whether the user lacks permission to speak in the voice channel.</summary>
    public bool Suppressed => this.Json.GetProperty("suppress").GetBoolean();

    /// <summary>Fetches the user associated with the voice connection.</summary>
    public ValueTask<User> GetUserAsync()
        => this.Bot.GetUserAsync(this.UserId);

    /// <summary>Fetches the voice channel the user is connected to.</summary>
    public ValueTask<IVoiceChannel> GetChannelAsync()
    {
        var channelId = this.Json.GetProperty("channel_id").ToSnowflake();
        return this.Bot.GetChannelAsync<IVoiceChannel>(channelId);
    }
    
    JsonElement IJsonEntity.Json => this.Json;
    Bot IBotEntity.Bot => this.Bot;
}
