namespace Donatello.Gateway;

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Gateway.Event;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon.Events;

// Declarations for user facing events.
public sealed partial class DiscordBot
{
    private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent;
    private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent;
    private AsynchronousEvent<UnknownEventContext> _unknownGatewayEvent;
    private AsynchronousEvent<ChannelCreatedEventContext> _channelCreateEvent;

    public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
    {
        add => _commandExecutedEvent.Hook(value);
        remove => _commandExecutedEvent.Unhook(value);
    }

    public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
    {
        add => _commandExecutionFailedEvent.Hook(value);
        remove => _commandExecutionFailedEvent.Unhook(value);
    }

    
    public event AsynchronousEventHandler<UnknownEventContext> UnknownEvent
    {
        add => _unknownGatewayEvent.Hook(value);
        remove => _unknownGatewayEvent.Unhook(value);
    }

    
    public event AsynchronousEventHandler<ChannelCreatedEventContext> ChannelCreated
    {
        add => _channelCreateEvent.Hook(value);
        remove => _channelCreateEvent.Unhook(value);
    }

    /// <summary>Receives gateway event payloads from each connected <see cref="DiscordShard"/>.</summary>
    private async Task DispatchGatewayEventsAsync(ChannelReader<DiscordEvent> eventReader)
    {
        await foreach (var gatewayEvent in eventReader.ReadAllAsync())
        {
            var shard = gatewayEvent.Shard;
            var eventName = gatewayEvent.Payload.GetProperty("t").GetString();
            var eventData = gatewayEvent.Payload.GetProperty("d");

            var eventTask = eventName switch
            {
                "CHANNEL_CREATE" => _channelCreateEvent.InvokeAsync(this, new ChannelCreatedEventContext()),
                _ => _unknownGatewayEvent.InvokeAsync(this, new UnknownEventContext(eventName, eventData))
            };

            await eventTask;
        }
    }

    private void InitializeEvents()
    {
        _commandExecutedEvent = new AsynchronousEvent<CommandExecutedEventArgs>(EventExceptionLogger);
        _commandService.CommandExecuted += (s, e) => _commandExecutedEvent.InvokeAsync(this, e);

        _commandExecutionFailedEvent = new AsynchronousEvent<CommandExecutionFailedEventArgs>(EventExceptionLogger);
        _commandService.CommandExecutionFailed += (s, e) => _commandExecutionFailedEvent.InvokeAsync(this, e);

        _unknownGatewayEvent = new AsynchronousEvent<UnknownEventContext>(EventExceptionLogger);
        _channelCreateEvent = new AsynchronousEvent<ChannelCreatedEventContext>(EventExceptionLogger);
    }

    private void EventExceptionLogger(Exception exception)
        => this.Logger.LogError(exception, "An event handler threw an exception.");
}
