namespace Donatello.Entity;

using Extension.Internal;
using Type;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>A post created by a user in a forum channel.</summary>
public class ForumPostChannel : GuildTextChannel, IThreadChannel
{
    public ForumPostChannel(Bot bot, JsonElement json) 
        : base(bot, json)
    {

    }

    /// <summary></summary>
    protected internal JsonCache FollowerCache { get; init; }

    /// <summary></summary>
    protected internal Snowflake ParentId => this.Json.GetProperty("parent_id").ToSnowflake();

    /// <summary>An additional sub-set of fields sent only with threads.</summary>
    protected internal JsonElement Metadata => this.Json.GetProperty("thread_metadata");

    /// <summary>Whether this post has been locked by a moderator.</summary>
    /// <remarks>Users will be unable to create replies to a locked post.</remarks>
    public bool Locked => this.Metadata.GetProperty("locked").GetBoolean();

    /// <summary>Whether this post has been closed and archived.</summary>
    /// <remarks>Users can reopen a post at any time by creating a new reply message.</remarks>
    public bool Closed => this.Metadata.GetProperty("archived").GetBoolean();

    /// <summary>The date when this post was created.</summary>
    public DateTimeOffset CreationDate => this.Metadata.GetProperty("create_timestamp").GetDateTimeOffset();

    /// <summary>Fetches the forum which contains this post.</summary>
    public ValueTask<GuildForumChannel> GetForumAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>Fetches the original message which started this post.</summary>
    public ValueTask<Message> GetTopicAsync()
    {
        throw new NotImplementedException();
    }

    public Task FollowAsync()
    {
        throw new NotImplementedException();
    }

    JsonElement IThreadChannel.Metadata => this.Metadata;
    Snowflake IThreadChannel.ParentId => this.ParentId;
    bool IThreadChannel.Locked => this.Locked;
    bool IThreadChannel.Archived => this.Closed;
    Task IThreadChannel.JoinAsync() => this.FollowAsync();
    Task IThreadChannel.LeaveAsync() => throw new NotImplementedException();
    Task IThreadChannel.AddMemberAsync(User user) => throw new NotImplementedException();
    Task IThreadChannel.RemoveMemberAsync(User user) => throw new NotImplementedException();
    ValueTask<ThreadMember> IThreadChannel.GetMemberAsync(Snowflake userId) => throw new NotImplementedException();
    Task<ReadOnlyCollection<ThreadMember>> IThreadChannel.GetMembersAsync() => throw new NotImplementedException();
    IAsyncEnumerable<ThreadMember> IThreadChannel.FetchMembersAsync() => throw new NotImplementedException();
}
