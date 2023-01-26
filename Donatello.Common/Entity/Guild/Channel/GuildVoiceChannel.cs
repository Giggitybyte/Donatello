﻿namespace Donatello.Entity;

using Donatello.Builder;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class GuildVoiceChannel : GuildChannel, IVoiceChannel, ITextChannel
{
    public GuildVoiceChannel(Bot bot, JsonElement json)
        : base(bot, json)
    {

    }

    /// <inheritdoc cref = "IVoiceChannel.Bitrate"/>
    public int Bitrate => this.Json.GetProperty("bitrate").GetInt32();

    /// <inheritdoc cref = "IVoiceChannel.UserLimit"/>
    public int UserLimit => this.Json.GetProperty("user_limit").GetInt32();

    public Task<Message> SendMessageAsync(MessageBuilder builder) => throw new System.NotImplementedException();

    public Task DeleteMessageAsync(Snowflake messageId) => throw new System.NotImplementedException();

    public IAsyncEnumerable<Message> GetMessagesAfterAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();

    public IAsyncEnumerable<Message> GetMessagesAroundAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();

    public IAsyncEnumerable<Message> GetMessagesAsync(ushort limit = 100) => throw new System.NotImplementedException();

    public IAsyncEnumerable<Message> GetMessagesBeforeAsync(Snowflake snowflake, ushort limit = 100) => throw new System.NotImplementedException();

    public IAsyncEnumerable<Message> GetPinnedMessagesAsync() => throw new System.NotImplementedException();

    
}

