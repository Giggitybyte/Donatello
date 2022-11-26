namespace Donatello.Builder;

using Donatello.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;

public abstract class EntityBuilder<TEntity> where TEntity : class, IJsonEntity
{
    public EntityBuilder()
    {
        this.Json = JsonNode.Parse("{}").AsObject();
    }

    public EntityBuilder(TEntity instance)
    {
        this.Json = JsonObject.Create(instance.Json);
    }

    /// <summary>Mutable JSON representation of a <typeparamref name="TEntity"/></summary>
    internal protected JsonObject Json { get; set; }

    /// <summary>Writes the fields of this builder to a JSON stream.</summary>
    internal void WriteTo(in Utf8JsonWriter jsonWriter)
        => this.Json.WriteTo(jsonWriter);
}
