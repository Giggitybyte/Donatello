namespace Donatello.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;

public sealed partial class Message
{
    /// <summary></summary>
    public sealed class Attachment : IJsonEntity
    {
        private readonly JsonElement _json;

        internal Attachment(JsonElement json)
        {
            _json = json;
        }

        /// <inheritdoc cref="IJsonEntity.Json"/>
        internal JsonElement Json => _json;

        /// <summary></summary>
        public ValueTask DownloadAsync()
            => throw new NotImplementedException();

        JsonElement IJsonEntity.Json => this.Json;
    }
}

