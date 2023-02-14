namespace Donatello.Common.Entity.Channel;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Builder;
using Message;

/// <summary>A channel that is not associated with a guild which allows for direct messages between two users.</summary>
public class DirectMessageChannel : Channel, ITextChannel
{
    public DirectMessageChannel(Bot bot, JsonElement json) 
        : base(bot, json) 
    { 

    }
}

