﻿namespace Donatello.Common.Entity.Guild;

using System;
using System.Text.Json;

public class Hub : Guild
{
    public Hub(Bot bot, JsonElement json) 
        : base(bot, json)
    {
        throw new NotImplementedException();
    }
}