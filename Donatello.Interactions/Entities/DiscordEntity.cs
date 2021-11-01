namespace Donatello.Interactions.Entities;

using System.Text.Json;

public abstract class DiscordEntity
{
    private readonly JsonElement _json;

    internal DiscordEntity(JsonElement json)
    {
        _json = json;
    }

    /// <summary>Backing JSON data for this entity.</summary>
    protected JsonElement Json => _json;

    /// <summary>(Most-likely) unique Discord ID.</summary>
    public ulong Id => ulong.Parse(_json.GetProperty("id").GetString());
}
