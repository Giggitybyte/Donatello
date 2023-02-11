namespace Donatello.Entity;

using Donatello;
using Extension.Internal;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Representation of a user's connection to a voice channel.</summary>
public sealed class DiscordVoiceState : IJsonEntity, IBotEntity
{
    private readonly Snowflake _guildId;

    public DiscordVoiceState(Bot bot, JsonElement entityJson, Snowflake guildId = null)
    {
        this.Json = entityJson;
        this.Bot = bot;

        if (entityJson.TryGetProperty("guild_id", out JsonElement prop))
            _guildId = prop.ToSnowflake();
        else
            _guildId = guildId;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    internal JsonElement Json { get; private set; }
    JsonElement IJsonEntity.Json => this.Json;

    /// <inheritdoc cref="IBotEntity.Bot"/>
    internal Bot Bot { get; private set; }
    Bot IBotEntity.Bot => this.Bot;

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
    public async ValueTask<User> GetUserAsync()
    {
        var userId = this.Json.GetProperty("user_id").ToSnowflake();

        if (_guildId is null)
            return await this.Bot.GetUserAsync(userId);
        else
        {
            var guild = await this.Bot.GetGuildAsync(_guildId);
            return await guild.GetMemberAsync(userId);
        }
    }

    /// <summary>Fetches the voice channel the user is connected to.</summary>
    public async Task<IVoiceChannel> GetChannelAsync()
    {
        IVoiceChannel channel = null;
        Snowflake channelId = this.Json.GetProperty("channel_id").ToSnowflake();

        if (_guildId is not null)
        {
            var guild = await this.Bot.GetGuildAsync(_guildId);
            channel = await guild.GetChannelAsync<GuildVoiceChannel>(channelId);
        }
        else
            channel = await this.Bot.GetChannelAsync<IVoiceChannel>(channelId);

        return channel;
    }
}
