namespace Donatello.Entity;

using Donatello.Builder;
using Donatello.Rest.Extension.Endpoint;
using Donatello.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A channel which can only contain threads; intended for organized topical conversations.</summary>
public class GuildForumChannel : GuildChannel
{
    protected internal GuildForumChannel(Bot bot, JsonElement json)
        : base(bot, json)
    {
        this.CachedPosts = new EntityCache<ForumPostChannel>();
    }

    /// <summary>Cached</summary>
    public EntityCache<ForumPostChannel> CachedPosts { get; private init; }

    /// <summary>Rules to follow when creating new posts.</summary>
    public string Guidelines => this.Json.TryGetProperty("topic", out JsonElement guidelines) ? guidelines.GetString() : string.Empty;

    /// <summary></summary>
    public async Task<ForumPostChannel> CreatePostAsync(MessageBuilder builder)
    {
        var (thread, message) = await this.Bot.RestClient.CreateForumThreadChannelAsync(this.Id, jsonWriter => builder.Json.WriteTo(jsonWriter), builder.Files);

        var post = new ForumPostChannel(this.Bot, thread);
        post.MessageCache.Add(new Message(this.Bot, message));

        return post;
    }

    /// <summary></summary>
    public Task<ForumPostChannel> CreatePostAsync(Action<MessageBuilder> builderDelegate)
    {
        var messageBuilder = new MessageBuilder();
        builderDelegate(messageBuilder);

        return this.CreatePostAsync(messageBuilder);
    }

    /// <summary></summary>
    public Task<ForumPostChannel> CreatePostAsync(string content)
        => this.CreatePostAsync(builder => builder.SetContent(content));

    /// <summary></summary>
    public async IAsyncEnumerable<ForumPostChannel> FetchActivePostsAsync()
    {
        var postsJson = await this.Bot.RestClient.GetActiveThreadsAsync(this.GuildId);
        foreach (var postJson in postsJson.GetProperty("threads").EnumerateArray())
        {
            var post = Channel.Create<ForumPostChannel>(postJson, this.Bot);
            yield return post;
        }
    }

    /// <summary></summary>
    public async ValueTask<ForumPostChannel> GetPostAsync(Snowflake postId)
    {
        if (this.CachedPosts.TryGet(postId, out ForumPostChannel post) is false)
            post = await this.FetchActivePostsAsync().FirstOrDefaultAsync(post => post.Id == postId);

        return post;
    }

    public IAsyncEnumerable<ForumPostChannel> GetOpenPostsAsync()
        => throw new NotImplementedException();

    public IAsyncEnumerable<ForumPostChannel> GetClosedPostsAsync()
        => throw new NotImplementedException();
}
