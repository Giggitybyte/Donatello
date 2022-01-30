namespace Donatello.Interactions.Model.Builder;
using System.Text.Json;

/// <summary></summary>
public abstract class EntityBuilder
{
    /// <summary>Writes the properties of this object to JSON.</summary>
    internal abstract void Build(Utf8JsonWriter json);
}
