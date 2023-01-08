namespace Donatello.Command;

using System;
using System.Threading.Tasks;

public abstract class DiscordCommand
{
    /// <summary>The name of this command.</summary>
    public string Name { get; protected init; }

    /// <summary>A summary of what the command does.</summary>
    public string Description { get; protected init; }

    /// <summary>Whether this command should be executed in a given context.</summary>
    /// <remarks>Inheriting command classes should override this method to run pre-execution checks.</remarks>
    protected internal virtual bool CanExecute(CommandContext context) 
        => true;

    /// <summary>Invoked </summary>
    protected internal virtual ValueTask SetupAsync(CommandContext context) 
        => ValueTask.CompletedTask;

    /// <summary>Invoked when a user runs this command.</summary>
    protected internal virtual ValueTask<CommandResult> ExecuteAsync(CommandContext context)
        => ValueTask.FromResult(CommandResult.Success());

    /// <summary></summary>
    protected internal virtual ValueTask HandleErrorAsync(CommandContext context, Exception exception)
        => ValueTask.CompletedTask;
}

public class GreetCommand : DiscordCommand
{
    public GreetCommand() 
    {
        this.Name = "greet";
        this.Description = """
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. 
            Duis pharetra urna eget tellus sagittis, non placerat odio sagittis. 
            Pellentesque a neque tincidunt, ullamcorper massa ac, scelerisque ipsum. 
            Donec fermentum ullamcorper magna, id eleifend leo accumsan nec. 
            Nullam lacinia ex risus, semper imperdiet neque lobortis nec. 
            Morbi ornare laoreet suscipit. 
            Proin molestie diam nec bibendum dapibus. 
            Interdum et malesuada fames ac ante ipsum primis in faucibus. 
            Maecenas mi nulla, finibus et mauris convallis, lacinia auctor mi.
            """;
    }

    public class BotCommand : DiscordCommand
    {
        public BotCommand()
        {
            this.Name = "bot";
            this.Description = "Short string here lmfao.";
        }

        protected internal override ValueTask<CommandResult> ExecuteAsync(CommandContext context) 
            => throw new NotImplementedException();

        protected internal override ValueTask HandleErrorAsync(CommandContext context, Exception exception) 
            => base.HandleErrorAsync(context, exception);
    }
}
