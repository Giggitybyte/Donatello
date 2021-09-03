using System;
using Donatello.Interactions.Commands.Enums;

namespace Donatello.Interactions.Commands.Attributes
{
    /// <summary>Explicitly specifies the type of a command.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class CommandTypeAttribute : Attribute
    {
        private readonly ApplicationCommandType _value;

        public CommandTypeAttribute(ApplicationCommandType value)
        {
            _value = value;
        }

        public ApplicationCommandType Value { get => _value; }
    }
}
