namespace Donatello.Interactions.Writer;

using System.Text.Json;

public abstract class PayloadWriter
{
    /// <summary>Writes the properties of this object to JSON.</summary>
    internal abstract void WritePayload(Utf8JsonWriter json);
}
