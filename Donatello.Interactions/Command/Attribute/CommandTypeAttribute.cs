namespace Donatello.Interactions.Command.Attribute;

using System;
using Donatello.Interactions.Entity.Enumeration;

/// <summary>Explicitly specifies the type of a command.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class CommandTypeAttribute : Attribute
{
    private readonly CommandType _value;

    public CommandTypeAttribute(CommandType value)
    {
        _value = value;
    }

    public CommandType Value { get => _value; }
}
