namespace Donatello.Interactions.Writer;

using System.Text.Json;

public abstract class PayloadWriter
{
    /// <summary>Writes the fields of this object to JSON.</summary>
    internal abstract void WriteJson(Utf8JsonWriter json);
}
