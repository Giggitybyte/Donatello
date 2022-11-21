namespace Donatello.Entity;

using System.Text.Json;

public class DiscordEmoji : IJsonEntity
{
    private JsonElement _json;
    private string _unicode;

    internal protected DiscordEmoji(JsonElement json)
    {
        this.Json = json;
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
    internal protected JsonElement Json { get; init; }

    /// <summary>Human readable name for this emoji.</summary>
    public string Name { get; internal init; }

    /// <summary></summary>
    public bool IsUnicode(out string value)
    {
        value = _unicode is not null
            ? _unicode
            : string.Empty;

        return value != string.Empty;
    }

    JsonElement IJsonEntity.Json => this.Json;
}
