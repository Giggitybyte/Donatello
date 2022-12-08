namespace Donatello.Entity;

using System.Text.Json;

public class DiscordEmoji
{
    private readonly string _unicode;

    internal protected DiscordEmoji(JsonElement json)
    {
        _json = json;
        var name = this.Json.GetProperty("name").GetString();

        if (json.TryGetProperty("id", out JsonElement prop) && prop.ValueKind is JsonValueKind.Null)
        {
            _unicode = name;

            if (EmojiDatabase.TryGetName(_unicode, out string emojiName))
                this.Name = emojiName;
        }
        else if (EmojiDatabase.TryGetUnicode(name, out string emoji))
        {
            this.Name = name;
            _unicode = emoji;
        }
        else
            _unicode = null;
    }

    /// <inheritdoc cref="IJsonEntity.Json"/>
    internal protected JsonElement Json => _json;

    /// <summary>Human readable name for this emoji.</summary>
    public string Name { get; internal init; }

    /// <summary>Returns <see langword="true"/> if this object represents a unicode emoji.</summary>
    /// <param name="value">If the method returns <see langword="true"/>, this parameter will contain an emoji string; otherwise it'll be <see cref="string.Empty"/>.</param>
    public bool IsUnicode(out string value)
    {
        value = _unicode is not null
            ? _unicode
            : string.Empty;

        return value != string.Empty;
    }

    JsonElement IJsonEntity.Json => this.Json;
}
