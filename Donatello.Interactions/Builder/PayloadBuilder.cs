namespace Donatello.Interactions.Builder;

using System.Text.Json;

public abstract class PayloadBuilder
{
    /// <summary>Writes the fields of this builder to JSON.</summary>
    internal abstract void Build(in Utf8JsonWriter json);
}
