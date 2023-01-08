namespace Donatello.Entity;

using Donatello.Extension.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class DiscordForumChannel : DiscordGuildChannel
{
    protected internal DiscordForumChannel(DiscordBot bot, JsonElement json)
        : base(bot, json)
    {

    }

    public IEnumerable<DiscordThreadChannel> CachedPosts
    {
        get
        {
            foreach (var thread in this.Bot.GuildCache[this.GuildId]?.ThreadCache?.Where(thread => thread.Json.GetProperty("parent_id").ToSnowflake() == this.Id))
                yield return thread;
        }
    }

    /// <summary>Rules to follow when creating new posts.</summary>
    public string Guidelines => this.Json.TryGetProperty("topic", out JsonElement guidelines) ? guidelines.GetString() : string.Empty;

    public Task<DiscordForumPostChannel> CreatePostAsync()
        => throw new NotImplementedException();

    /// <summary></summary>
    public Task<DiscordForumPostChannel> GetPostAsync()
    {

    }

    public IAsyncEnumerable<DiscordThreadChannel> GetOpenPostsAsync()
        => throw new NotImplementedException();

    public IAsyncEnumerable<DiscordThreadChannel> GetClosedPostsAsync()
        => throw new NotImplementedException();
}
