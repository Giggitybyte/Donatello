namespace Donatello.Rest;

using Donatello.Rest.Routes;

/// <summary>Abstraction for the Discord REST API.</summary>
public sealed class DiscordRestApi
{
    private readonly DiscordHttpClient _discordHttpClient;

    /// <summary></summary>
    /// <param name="token"></param>
    /// <param name="isBearer">Whether the provided token is an OAuth2 bearer token.</param>
    public DiscordRestApi(string token, bool isBearer = false)
    {
        _discordHttpClient = new(token, isBearer);

        this.Application = new(_discordHttpClient);
        this.Channel = new(_discordHttpClient);
        this.Guild = new(_discordHttpClient);
        this.Invite = new(_discordHttpClient);
        this.Stage = new(_discordHttpClient);
        this.Sticker = new(_discordHttpClient);
    }

    /// <summary>Exposes endpoints for application commands.</summary>
    public ApplicationRoute Application { get; init; }

    /// <summary>Exposes endpoints for channels.</summary>
    public ChannelRoute Channel { get; init; }

    /// <summary>Exposes endpoints for guilds.</summary>
    public GuildRoute Guild { get; init; }

    /// <summary>Exposes endpoints for guild invites.</summary>
    public InviteRoute Invite { get; init; }

    /// <summary>Exposes endpoints for guild stage channels.</summary>
    public StageRoute Stage { get; init; }

    /// <summary>Exposes endpoints for sticker packs.</summary>
    /// <remarks>For guild sticker endpoints, use <c><see cref="Guild"/></c>.</remarks>
    public StickerRoute Sticker { get; init; }

    /// <summary>Exposes endpoints for users.</summary>
    public UserRoute User { get; init; }

    /// <summary>Exposes endpoints for webhooks.</summary>
    public WebhookRoute Webhook { get; init; }
}
