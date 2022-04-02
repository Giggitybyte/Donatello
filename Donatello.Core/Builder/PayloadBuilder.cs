namespace Donatello.Entity.Builder;

using System.Text.Json;

public abstract class PayloadBuilder
{
    /// <summary>Writes the fields of this builder to JSON.</summary>
    internal abstract void WriteJson(in Utf8JsonWriter json);
}
