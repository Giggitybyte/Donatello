﻿namespace Donatello.Gateway;

using System;
using Donatello.Gateway.Event;
using Qmmands;
using Qommon.Events;

// Declarations for all user facing events.
public sealed partial class DiscordBot
{
    private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent = new(EventExceptionLogger);
    public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
    {
        add => _commandExecutedEvent.Hook(value);
        remove => _commandExecutedEvent.Unhook(value);
    }

    private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent = new(EventExceptionLogger);
    public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
    {
        add => _commandExecutionFailedEvent.Hook(value);
        remove => _commandExecutionFailedEvent.Unhook(value);
    }

    private AsynchronousEvent<MessageReceivedEventContext> _gatewayMessageReceivedEvent = new(EventExceptionLogger);
    public event AsynchronousEventHandler<MessageReceivedEventContext> MessageReceived
    {
        add => _gatewayMessageReceivedEvent.Hook(value);
        remove => _gatewayMessageReceivedEvent.Unhook(value);
    }

    private static void EventExceptionLogger(Exception exception)
    {
        // this.Logger.Log(...);
        throw new NotImplementedException();
    }
}