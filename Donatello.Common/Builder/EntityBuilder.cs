namespace Donatello.Common.Builder;

using System.Collections.Generic;
using System.Text.Json.Nodes;
using Donatello.Rest;

/// <summary></summary>
public abstract class EntityBuilder
{
    protected EntityBuilder()
    {
        this.Json = new JsonObject();
        this.Files = new List<FileAttachment>();
    }

    /// <summary></summary>
    protected internal List<FileAttachment> Files { get; private init; }

    /// <summary></summary>
    protected internal JsonObject Json { get; private init; }
}
