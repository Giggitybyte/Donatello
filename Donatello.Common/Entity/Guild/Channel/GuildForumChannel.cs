namespace Donatello.Common.Entity.Guild.Channel;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Builder;
using Common.Entity.Channel;
using Message;

/// <summary>A channel which can only contain threads; intended for organized topical conversations.</summary>
public class GuildForumChannel : GuildChannel
{
    protected internal GuildForumChannel(Bot bot, JsonElement json)
        : base(bot, json)
    {        
    }

    public GuildForumChannel(Bot bot, JsonElement entityJson, Snowflake guildId) 
        : base(bot, entityJson, guildId)
    {
    }

    /// <summary>Cached thread channel instances.</summary>
    public EntityCache<GuildThreadChannel> ThreadCache { get; } = new EntityCache<GuildThreadChannel>();

    /// <summary>Rules to follow when creating new threads.</summary>
    public string Guidelines => this.Json.TryGetProperty("topic", out JsonElement guidelines) ? guidelines.GetString() : string.Empty;

    /// <summary>Creates a new thread with a starting message.</summary>
    public async Task<GuildThreadChannel> CreateThreadAsync(MessageBuilder builder)
    {
        var (thread, message) = await this.Bot.RestClient.CreateForumThreadChannelAsync(this.Id, jsonWriter => builder.Json.WriteTo(jsonWriter), builder.Files);

        var post = new GuildThreadChannel(this.Bot, thread);
        post.MessageCache.Add(new Message(this.Bot, message));

        return post;
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
    public async IAsyncEnumerable<GuildThreadChannel> FetchActiveThreadsAsync()
    {
        var postsJson = await this.Bot.RestClient.GetActiveThreadsAsync(this.GuildId);
        foreach (var postJson in postsJson.GetProperty("threads").EnumerateArray())
        {
            var post = Channel.Create<GuildThreadChannel>(this.Bot, postJson);
            this.ThreadCache.Add(post);
            yield return post;
        }
    }

    public async IAsyncEnumerable<GuildThreadChannel> FetchArchivedThreadsAsync()
    {
        var postsJson = await this.Bot.RestClient.GetArchivedPublicThreadsAsync(this.GuildId);
        foreach (var postJson in postsJson.GetProperty("threads").EnumerateArray())
        {
            var post = Channel.Create<GuildThreadChannel>(this.Bot, postJson);
            this.ThreadCache.Add(post);
            yield return post;
        }
    }

    /// <summary></summary>
    public async ValueTask<GuildThreadChannel> GetThreadAsync(Snowflake threadId)
    {
        if (this.ThreadCache.TryGet(threadId, out GuildThreadChannel post) is false)
            post = await this.FetchActiveThreadsAsync().FirstOrDefaultAsync(post => post.Id == threadId);

        return post;
    }

    /// <summary></summary>
    public IAsyncEnumerable<GuildThreadChannel> GetActiveThreadsAsync()
        => throw new NotImplementedException();

    /// <summary></summary>
    public IAsyncEnumerable<GuildThreadChannel> GetArchivedThreadsAsync()
        => throw new NotImplementedException();
}
