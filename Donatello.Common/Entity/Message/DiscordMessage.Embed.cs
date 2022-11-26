namespace Donatello.Entity;

using System.Text.Json;

public sealed partial class DiscordMessage
{
    /// <summary></summary>
    public sealed class Embed : IJsonEntity
    {
        private readonly JsonElement _json;

        internal Embed(JsonElement json)
        {
            _json = json;
        }

        /// <inheritdoc cref="IJsonEntity.Json"/>
        internal JsonElement Json => _json;
        
        /// <summary></summary>
        /// <param name="title"></param>
        public bool HasTitle(out string title)
        {
            title = _json.TryGetProperty("title", out JsonElement prop)
                ? prop.ToString()
                : string.Empty;

            return title != string.Empty;
        }

        /// <summary></summary>
        /// <param name="description"></param>
        public bool HasDescription(out string description)
        {
            description = _json.TryGetProperty("description", out JsonElement prop)
                ? prop.ToString()
                : string.Empty;

            return description != string.Empty;
        }

        JsonElement IJsonEntity.Json => throw new System.NotImplementedException();
    }
}

