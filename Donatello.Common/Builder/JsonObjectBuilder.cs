namespace Donatello.Builder;

using Donatello.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;

public abstract class JsonObjectBuilder<TEntity> where TEntity : class, ISnowflakeEntity
{
    public JsonObjectBuilder()
    {
        this.Json = JsonNode.Parse("{}").AsObject();
    }

    public JsonObjectBuilder(TEntity instance)
    {
        this.Json = JsonObject.Create(instance.Json);
    }

    /// <summary>Mutable JSON representation of an entity.</summary>
    protected JsonObject Json { get; set; }

    public List<Attachment>

    /// <summary>Writes the fields of this builder to JSON.</summary>
    internal void Build(in Utf8JsonWriter jsonWriter)
        => this.Json.WriteTo(jsonWriter);
}
