namespace Donatello.Builder;

using Donatello.Rest;
using System.Collections.Generic;
using System.Text.Json.Nodes;

/// <summary></summary>
public abstract class EntityBuilder
{
    public EntityBuilder()
    {
        this.Json = new JsonObject();
        this.Files = new List<FileAttachment>();
    }

    /// <summary></summary>
    internal protected List<FileAttachment> Files { get; private init; }

    /// <summary></summary>
    internal protected JsonObject Json { get; private init; }
}
