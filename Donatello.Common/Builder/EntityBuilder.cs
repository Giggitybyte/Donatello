namespace Donatello.Entity.Builder;

using System.Text.Json;

public abstract class EntityBuilder
{
    /// <summary>Writes the fields of this builder to JSON.</summary>
    internal abstract void Build(in Utf8JsonWriter jsonWriter);
}
