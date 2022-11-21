namespace Donatello.Entity;

using System;
using System.Text.Json;

public partial class DiscordUser
{
    public sealed class Activity : IJsonEntity
    {
        private readonly JsonElement _json;

        internal Activity(JsonElement json)
        {
            _json = json;
        }

        /// <summary></summary>
        internal JsonElement Json => _json;
        

        /// <summary>The name of the activity.</summary>
        public string Name => this.Json.GetProperty("name").GetString();

        /// <summary>A summary of what the user is doing.</summary>
        public string Details => this.Json.TryGetProperty("details", out JsonElement prop) ? prop.ToString() : string.Empty;

        /// <summary></summary>
        public bool HasTimestamp(out DateTime startTime, out DateTime endTime)
        {

        }

        JsonElement IJsonEntity.Json => this.Json;
    }
}
