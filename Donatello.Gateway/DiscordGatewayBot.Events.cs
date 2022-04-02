namespace Donatello.Gateway;

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Donatello.Gateway.Event;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon.Events;

// Everything event related; C# declarations, Discord dispatch.
public sealed partial class DiscordGatewayBot
{
    private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent;
    private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent;
    private AsynchronousEvent<UnknownEventContext> _unknownGatewayEvent;
    private AsynchronousEvent<ChannelCreatedEventContext> _channelCreateEvent;

    /// <summary></summary>
    public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
    {
        add => _commandExecutedEvent.Hook(value);
        remove => _commandExecutedEvent.Unhook(value);
    }

    /// <summary></summary>
    public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
    {
        add => _commandExecutionFailedEvent.Hook(value);
        remove => _commandExecutionFailedEvent.Unhook(value);
    }

    /// <summary></summary>
    public event AsynchronousEventHandler<UnknownEventContext> UnknownEvent
    {
        add => _unknownGatewayEvent.Hook(value);
        remove => _unknownGatewayEvent.Unhook(value);
    }

    /// <summary></summary>
    public event AsynchronousEventHandler<ChannelCreatedEventContext> ChannelCreated
    {
        add => _channelCreateEvent.Hook(value);
        remove => _channelCreateEvent.Unhook(value);
    }

    /// <summary></summary>
    private void InitializeEvents()
    {
        _commandExecutedEvent = new AsynchronousEvent<CommandExecutedEventArgs>(EventExceptionLogger);
        _commandService.CommandExecuted += (s, e) => _commandExecutedEvent.InvokeAsync(this, e);

        _commandExecutionFailedEvent = new AsynchronousEvent<CommandExecutionFailedEventArgs>(EventExceptionLogger);
        _commandService.CommandExecutionFailed += (s, e) => _commandExecutionFailedEvent.InvokeAsync(this, e);

        _unknownGatewayEvent = new AsynchronousEvent<UnknownEventContext>(EventExceptionLogger);
        _channelCreateEvent = new AsynchronousEvent<ChannelCreatedEventContext>(EventExceptionLogger);
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
                "CHANNEL_UPDATE" => throw new NotImplementedException(),
                "CHANNEL_DELETE" => throw new NotImplementedException(),
                "CHANNEL_PINS_UPDATE" => throw new NotImplementedException(),
                "THREAD_CREATE" => throw new NotImplementedException(),
                "THREAD_UPDATE" => throw new NotImplementedException(),
                "THREAD_DELETE" => throw new NotImplementedException(),
                "THREAD_LIST_SYNC" => throw new NotImplementedException(),
                "THREAD_MEMBER_UPDATE" => throw new NotImplementedException(),
                "GUILD_CREATE" => throw new NotImplementedException(),
                "GUILD_UPDATE" => throw new NotImplementedException(),
                "GUILD_DELETE" => throw new NotImplementedException(),
                "GUILD_BAN_ADD" => throw new NotImplementedException(),
                "GUILD_BAN_REMOVE" => throw new NotImplementedException(),
                "GUILD_EMOJIS_UPDATE" => throw new NotImplementedException(),
                "GUILD_STICKERS_UPDATE" => throw new NotImplementedException(),
                "GUILD_INTREGRATIONS_UPDATE" => throw new NotImplementedException(),
                "GUILD_MEMBER_ADD" => throw new NotImplementedException(),
                "GUILD_MEMBER_REMOVE" => throw new NotImplementedException(),
                "GUILD_MEMBER_UPDATE" => throw new NotImplementedException(),
                "GUILD_MEMBERS_CHUNK" => throw new NotImplementedException(),
                "GUILD_ROLE_CREATE" => throw new NotImplementedException(),
                "GUILD_ROLE_DELETE" => throw new NotImplementedException(),
                "GUILD_SCHEDULED_EVENT_CREATE" => throw new NotImplementedException(),
                "GUILD_SCHEDULED_EVENT_UPDATE" => throw new NotImplementedException(),
                "GUILD_SCHEDULED_EVENT_DELETE" => throw new NotImplementedException(),
                "GUILD_SCHEDULED_EVENT_USER_ADD" => throw new NotImplementedException(),
                "GUILD_SCHEDULED_EVENT_USER_REMOVE" => throw new NotImplementedException(),
                "INTEGRATION_CREATE" => throw new NotImplementedException(),
                "INTEGRATION_UPDATE" => throw new NotImplementedException(),
                "INTEGRATION_DELETE" => throw new NotImplementedException(),
                "INTERACTION_CREATE" => throw new NotImplementedException(),
                "INVITE_CREATE" => throw new NotImplementedException(),
                "INVITE_DELETE" => throw new NotImplementedException(),
                "MESSAGE_CREATE" => throw new NotImplementedException(),
                "MESSAGE_UPDATE" => throw new NotImplementedException(),
                "MESSAGE DELETE" => throw new NotImplementedException(),
                "MESSAGE_DELETE_BULK" => throw new NotImplementedException(),
                "MESSAGE_REACTION_ADD" => throw new NotImplementedException(),
                "MESSAGE_REACTION_REMOVE" => throw new NotImplementedException(),
                "MESSAGE_REACTION_REMOVE_EMOJI" => throw new NotImplementedException(),
                "PRESENSE_UPDATE" => throw new NotImplementedException(),
                "STAGE_INSTANCE_CREATE" => throw new NotImplementedException(),
                "STAGE_INSTANCE_UPDATE" => throw new NotImplementedException(),
                "STAGE_INSTANCE_DELETE" => throw new NotImplementedException(),
                "TYPING_START" => throw new NotImplementedException(),
                "USER_UPDATE" => throw new NotImplementedException(),
                "VOICE_STATE_UPDATE" => throw new NotImplementedException(),
                "VOICE_SERVER_UPDATE" => throw new NotImplementedException(),
                "WEBHOOKS_UPDATE" => throw new NotImplementedException(),
                _ => UnknownEvent()
            };

            await eventTask;

            ValueTask UnknownEvent()
            {
                this.Logger.LogWarning("Received unknown gateway event: {EventName}", eventName);
                return _unknownGatewayEvent.InvokeAsync(this, new UnknownEventContext(eventName, eventData));
            }
        }
    }

    /// <summary></summary>
    private void EventExceptionLogger(Exception exception)
        => this.Logger.LogError(exception, "An event handler threw an exception.");
}
