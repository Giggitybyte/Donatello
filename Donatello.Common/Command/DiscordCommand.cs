namespace Donatello.Command;

using System;
using System.Threading.Tasks;

public abstract class DiscordCommand
{
    /// <summary></summary>
    public string Name { get; protected init; }

    /// <summary></summary>
    public string Description { get; protected init; }

    /// <summary></summary>
    protected internal abstract ValueTask<CommandResult> ExecuteAsync();

    /// <summary></summary>
    protected internal virtual bool CanExecute(CommandContext context) 
        => true;

    /// <summary></summary>
    protected internal virtual ValueTask SetupAsync(CommandContext context) 
        => ValueTask.CompletedTask;

    /// <summary></summary>
    protected internal virtual ValueTask ErrorAsync(CommandContext context, Exception exception)
        => ValueTask.CompletedTask;
}
