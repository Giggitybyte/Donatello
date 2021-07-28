using Donatello.Websocket.Event;
using Qmmands;
using Qommon.Events;

namespace Donatello.Websocket
{
    public sealed partial class DiscordBot
    {
        private AsynchronousEvent<CommandExecutedEventArgs> _commandExecutedEvent = new();
        public event AsynchronousEventHandler<CommandExecutedEventArgs> CommandExecuted
        {
            add => _commandExecutedEvent.Hook(value);
            remove => _commandExecutedEvent.Unhook(value);
        }

        private AsynchronousEvent<CommandExecutionFailedEventArgs> _commandExecutionFailedEvent = new();
        public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> CommandExecutionFailed
        {
            add => _commandExecutionFailedEvent.Hook(value);
            remove => _commandExecutionFailedEvent.Unhook(value);
        }

        private AsynchronousEvent<MessageReceivedEventPayload> _gatewayMessageReceivedEvent = new();
        public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> MessageReceived
        {
            add => _commandExecutionFailedEvent.Hook(value);
            remove => _commandExecutionFailedEvent.Unhook(value);
        }
    }
}
