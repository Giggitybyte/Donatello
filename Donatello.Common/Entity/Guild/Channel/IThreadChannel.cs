namespace Donatello.Entity;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

public interface IThreadChannel : IGuildChannel, ITextChannel
{
    protected internal Snowflake ParentId { get; }

    protected internal JsonElement Metadata { get; }

    protected internal bool Locked { get; }

    protected internal bool Archived { get; }

    public Task JoinAsync();

    public Task LeaveAsync();

    public Task AddMemberAsync(User user);

    public ValueTask<ThreadMember> GetMemberAsync(Snowflake userId);

    public Task<ReadOnlyCollection<ThreadMember>> GetMembersAsync();

    public IAsyncEnumerable<ThreadMember> FetchMembersAsync();

    public Task RemoveMemberAsync(User user);
}
