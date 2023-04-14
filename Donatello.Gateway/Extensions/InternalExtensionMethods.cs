namespace Donatello.Gateway.Extensions;

using System;
using System.Text.Json;
using Event;

internal static class InternalExtensionMethods
{
    internal static EntityCreatedEvent ToChannelEvent(this JsonElement eventJson)
    {
        throw new NotImplementedException();
    }
}