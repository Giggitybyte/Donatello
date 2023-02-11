namespace Donatello.Interaction.Command.Attribute;

using System;

/// <summary>Explicitly specifies the type of a command.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class CommandTypeAttribute : Attribute
{
    private readonly CommandType _value;

    public CommandTypeAttribute(CommandType value)
    {
        _value = value;
    }

    public CommandType Value => _value;
}
