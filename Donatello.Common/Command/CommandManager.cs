namespace Donatello.Command;

using System;
using System.Collections.Generic;

internal class CommandManager
{
    private struct Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<CommandContext,>
        public Command[] Subcommands { get; set; }
    }

    private Dictionary<string, Command> _commands;
}
