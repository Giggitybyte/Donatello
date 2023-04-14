namespace Donatello.Common.Entity;

using System.Text.Json;

/// <summary>An object containing a <see cref="JsonElement"/> instance of a Discord entity.</summary>
public interface IJsonEntity
{
    /// <summary>JSON representation of this object.</summary>
    protected internal JsonElement Json { get; }
}
