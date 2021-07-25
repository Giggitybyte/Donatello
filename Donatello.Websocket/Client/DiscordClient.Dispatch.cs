using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Donatello.Websocket.Client
{
    // Determines the type of an incoming event dispatch and invokes a corresponding user event.
    public sealed partial class DiscordClient
    {
        private async Task HandleEventDispatch(JsonElement data)
        {
            throw new NotImplementedException();
        }
    }
}
