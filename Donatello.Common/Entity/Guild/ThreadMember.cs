namespace Donatello.Common.Entity.Guild;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Channel;
using Extension;
using User;

/// <summary></summary>
public sealed class ThreadMember : GuildEntity
{
    public ThreadMember(JsonElement threadMemberJson, Bot bot)
        : base(threadMemberJson, bot)
    {
    }

    /// <summary>Discord user ID of this member.</summary>
    public new Snowflake Id => this.Json.GetProperty("user_id").ToSnowflake();

    /// <summary>ID of the thread which this member belongs to.</summary>
    public Snowflake ThreadId => base.Id;

    /// <summary>When the member was added to the thread.</summary>
    public DateTimeOffset JoinDate => this.Json.GetProperty("joined_at").GetDateTimeOffset();

    /// <summary></summary>
    public ValueTask<User> GetUserAsync()
        => this.Bot.GetUserAsync(this.Id);

    /// <summary>Fetches the thread this member belongs to.</summary>
    public async ValueTask<GuildThreadChannel> GetThreadAsync()
    {
        var guild = await this.GetGuildAsync();
        var threadId = this.Json.GetProperty("id").ToSnowflake();
        var thread = await guild.GetThreadAsync(threadId);

        return thread;
    }
}