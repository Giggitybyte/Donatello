namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Builder;
using Donatello.Rest.Extension.Endpoint;
using Extension;

/// <summary>A channel which can only contain threads; intended for organized conversations.</summary>
public class GuildForumChannel : GuildChannel
{
    public GuildForumChannel(JsonElement json, Bot bot) : base(json, bot) { }
    public GuildForumChannel(JsonElement entityJson, Snowflake id, Bot bot) : base(entityJson, id, bot) { }

    /// <summary>Rules to follow when creating new threads.</summary>
    public string Guidelines => this.Json.TryGetProperty("topic", out JsonElement guidelines) ? guidelines.GetString() : string.Empty;

    /// <summary>Creates a new thread with a starting message.</summary>
    public async Task<GuildThreadChannel> CreateThreadAsync(MessageBuilder builder)
    {
        var (threadJson, messageJson) = await this.Bot.RestClient.CreateForumThreadChannelAsync(
            this.Id, 
            jsonWriter => builder.Json.WriteTo(jsonWriter), 
            builder.Files
        );
        
        this.Bot.MessageCache.AddOrUpdate(messageJson);
        return new GuildThreadChannel(threadJson, this.Bot);
    }

    /// <inheritdoc cref="CreateThreadAsync(Donatello.Common.Builder.MessageBuilder)"/>
    public Task<GuildThreadChannel> CreateThreadAsync(Action<MessageBuilder> builderDelegate)
    {
        var messageBuilder = new MessageBuilder();
        builderDelegate(messageBuilder);
        return this.CreateThreadAsync(messageBuilder);
    }

    /// <inheritdoc cref="CreateThreadAsync(Donatello.Common.Builder.MessageBuilder)"/>
    public Task<GuildThreadChannel> CreateThreadAsync(string content)
        => this.CreateThreadAsync(builder => builder.SetContent(content));

    /// <summary></summary>
    public IAsyncEnumerable<GuildThreadChannel> FetchActiveThreadsAsync()
        => this.FetchThreadsAsync(this.Bot.RestClient.GetActiveThreadsAsync(this.GuildId));

    public IAsyncEnumerable<GuildThreadChannel> FetchArchivedThreadsAsync()
        => this.FetchThreadsAsync(this.Bot.RestClient.GetArchivedPublicThreadsAsync(this.GuildId));

    private async IAsyncEnumerable<GuildThreadChannel> FetchThreadsAsync(Task<JsonElement> requestTask)
    {
        JsonElement jsonThreads = await requestTask;
        foreach (var threadJson in jsonThreads.GetProperty("threads").EnumerateArray())
        {
            this.Bot.GuildThreadCache[this.GuildId].AddOrUpdate(threadJson);
            yield return threadJson.AsChannel<GuildThreadChannel>(this.Bot);
        }
    }

    /// <summary></summary>
    public async ValueTask<GuildThreadChannel> GetThreadAsync(Snowflake threadId)
    {
        if (this.Bot.GuildThreadCache[this.GuildId].TryGetEntry(threadId, out JsonElement threadJson))
            return threadJson.AsChannel<GuildThreadChannel>(this.Bot);
        else
            return await this.FetchActiveThreadsAsync().FirstOrDefaultAsync(post => post.Id == threadId);
    }

    /// <summary></summary>
    public IAsyncEnumerable<GuildThreadChannel> GetActiveThreadsAsync()
        => throw new NotImplementedException();

    /// <summary></summary>
    public IAsyncEnumerable<GuildThreadChannel> GetArchivedThreadsAsync()
        => throw new NotImplementedException();
}
