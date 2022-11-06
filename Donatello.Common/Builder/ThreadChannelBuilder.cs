namespace Donatello.Entity.Builder;

using System.Text.Json;

public sealed class ThreadChannelBuilder : GuildChannelBuilder
{
    internal override void ConstructJson(in Utf8JsonWriter jsonWriter)
    {
        throw new System.NotImplementedException();
    }
}
