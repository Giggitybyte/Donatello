namespace Donatello.Interactions.Model.Builder;

using System.IO;
using System.Text.Json;

/// <summary></summary>
public abstract class EntityBuilder
{
    /// <summary>Writes the properties of this object to a JSON UTF8 stream.</summary>
    internal virtual Stream ToJsonStream()
    {
        var jsonStream = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(jsonStream);

        this.JsonWriter.Flush();
        _jsonStream.Seek(0, SeekOrigin.Begin);

        return _jsonStream;
    }
}
