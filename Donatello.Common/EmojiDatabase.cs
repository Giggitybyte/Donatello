namespace Donatello.Common;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A lookup for all default emojis available on Discord.</summary>
/// <remarks>
/// All emoji data is fetched from the
/// <a href="https://emzi0767.gl-pages.emzi0767.dev/discord-emoji/">Emoji Map Project</a>
/// by <a href="https://emzi0767.com/">Emzi0767</a>.
/// </remarks>
public class EmojiDatabase
{
    private static readonly string _emojiMapUrl = "https://emzi0767.gl-pages.emzi0767.dev/discord-emoji/discordEmojiMap.min.json";
    private static readonly HttpClient _httpClient;
    private static EmojiDatabase _instance;
    
    private Dictionary<string, Emoji> _unicodeEmojis;
    private Dictionary<Emoji, string> _emojiShortcodes;

    static EmojiDatabase()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(10)
        };
        
        _httpClient = new HttpClient(handler);
    }

    private EmojiDatabase()
    {
        _unicodeEmojis = new Dictionary<string, Emoji>();
        _emojiShortcodes = new Dictionary<Emoji, string>();
    }

    /// <summary>Date when this instance was created.</summary>
    public DateTimeOffset CreationDate { get; private init; }

    /// <summary>Date when emojis were last retrieved from Discord.</summary>
    public DateTimeOffset LastUpdated { get; private init; }

    /// <summary>Returns an instance of the database.</summary>
    /// <remarks>
    /// If an instance was created within the last 4 hours, that instance will be returned.
    /// Otherwise, an up-to-date emoji map will be downloaded then parsed to create a new instance.
    /// </remarks>
    public static async ValueTask<EmojiDatabase> GetInstanceAsync()
    {
        if (_instance is null || DateTimeOffset.Now - _instance.CreationDate > TimeSpan.FromHours(4))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Donatello/0.0.0 (Author 176477523717259264)");
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, _emojiMapUrl));
            
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var responseJson = await JsonDocument.ParseAsync(responseStream);
            var emojiMapJson = responseJson.RootElement;

            var instance = new EmojiDatabase
            {
                CreationDate = DateTimeOffset.Now,
                LastUpdated = emojiMapJson.GetProperty("versionTimestamp").GetDateTimeOffset()
            };

            foreach (var emojiJson in emojiMapJson.GetProperty("emojiDefinitions").EnumerateArray())
            {
                var emoji = new Emoji(emojiJson.Clone());
                instance._unicodeEmojis.Add(emoji.Shortcode, emoji);
                instance._emojiShortcodes.Add(emoji, emoji.Shortcode);
            }

            _instance = instance;
        }
        
        return _instance;
    }

    /// <summary>Returns <see langword="true"/> if the provided unicode emoji string has a matching shortcode in the database.</summary>
    /// <param name="emoji">Unicode code point string.</param>
    /// <param name="shortcode">If the method returns <see langword="true"/>, this parameter will contain an emoji shortcode; otherwise it'll be <see cref="string.Empty"/>.</param>
    public bool TryGetShortcode(Emoji emoji, out string shortcode)
        => _emojiShortcodes.TryGetValue(emoji, out shortcode);

    /// <summary>Returns <see langword="true"/> if the provided shortcode has a matching unicode emoji in the database.</summary>
    /// <param name="shortcode">An emoji shortcode.</param>
    /// <param name="emoji">If the method returns <see langword="true"/>, this parameter will contain a unicode emoji string; otherwise it'll be <see langword="null"/>.</param>
    public bool TryGetEmoji(string shortcode, out Emoji emoji)
    {
        if (shortcode.StartsWith(':') is false) shortcode = ":" + shortcode;
        if (shortcode.EndsWith(':') is false) shortcode = shortcode + ":";
        return _unicodeEmojis.TryGetValue(shortcode, out emoji);
    }
}
