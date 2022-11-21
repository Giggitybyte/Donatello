namespace Donatello.Command;

using Donatello.Builder;
using Donatello.Entity;
using System;

public struct CommandResult
{
    /// <summary></summary>
    internal DiscordMessage Response { get; private set; }

    /// <summary></summary>
    internal Exception Exception { get; private set; }

    public static CommandResult Success()
    {

    }

    public static CommandResult Success(Action<MessageBuilder> response)
    {

    }

    public static CommandResult Failure()
    {

    }

    public static CommandResult Failure(Action<Exception> response)
    {

    }

    public static CommandResult Error(Exception exception)
    {

    }

    public static CommandResult Error(Exception exception, Action<Exception> response)
    {

    }
}
