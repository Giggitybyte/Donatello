using Donatello.Websocket.EventContext;
using Qmmands;
using Qommon.Events;

namespace Donatello.Websocket.Bot
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

        private AsynchronousEvent<MessageReceivedEventContext> _gatewayMessageReceivedEvent = new();
        public event AsynchronousEventHandler<CommandExecutionFailedEventArgs> MessageReceived
        {
            add => _commandExecutionFailedEvent.Hook(value);
            remove => _commandExecutionFailedEvent.Unhook(value);
        }
    }
}
