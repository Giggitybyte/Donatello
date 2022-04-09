namespace Donatello.Entity;

using System.Text.Json;

/// <summary></summary>
public class DiscordApplication : DiscordEntity
{
    public DiscordApplication(DiscordApiBot bot, JsonElement json) : base(bot, json) { }

    /// <summary></summary>
    public string Name => this.Json.GetProperty("name").GetString();

    /// <summary>Application icon URL.</summary>
    /// <remarks>May return <see cref="string.Empty"/> if an icon has not been uploaded for this application.</remarks>
    public string IconUrl
    {
        get
        {
            var iconHash = this.Json.GetProperty("icon");
            if (iconHash.ValueKind is not JsonValueKind.Null)
                return $"https://cdn.discordapp.com/app-icons/{this.Id}/{iconHash.GetString()}.png";
            else
                return string.Empty;
        }
    }

    /// <summary>Summary of what the application does.</summary>
    public string Description => this.Json.GetProperty("description").GetString();
}

