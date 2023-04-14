namespace Donatello.Common;

using System;
using System.Globalization;
using System.Text.Json.Nodes;

/// <summary></summary>
public class Snowflake : IComparable<Snowflake>
{
    private static readonly DateTimeOffset _discordEpoch = new(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <param name="rawValue">64-bit integer representing a Discord snowflake.</param>
    private Snowflake(ulong rawValue)
    {
        this.CreationDate = _discordEpoch.AddMilliseconds(rawValue >> 22);
        this.InternalWorkerId = (byte)((rawValue & 0x3E0000) >> 17);
        this.InternalProcessId = (byte)((rawValue & 0x1F000) >> 12);
        this.InternalIncrement = (ushort)(rawValue & 0xFFF);

        this.Value = rawValue;
    }

    /// <summary>64-bit integer representation of this snowflake.</summary>
    public ulong Value { get; }

    /// <summary>The date this snowflake was generated.</summary>
    public DateTimeOffset CreationDate { get; }

    /// <summary>The internal worker's ID that was used by Discord to generate the snowflake.</summary>
    public byte InternalWorkerId { get; }

    /// <summary>The internal process ID that was used by Discord to generate the snowflake.</summary>
    public byte InternalProcessId { get; }

    /// <summary>A number incremented by Discord every time a snowflake is generated.</summary>
    public ushort InternalIncrement { get; }

    /// <summary>Returns the 64-bit integer representation of this snowflake as a string.</summary>
    public override string ToString()
        => this.Value.ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public override bool Equals(object obj)
        => obj is Snowflake snowflake && snowflake == this;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.Value, this.CreationDate, this.InternalWorkerId, this.InternalProcessId, this.InternalIncrement);

    /// <inheritdoc/>
    public int CompareTo(Snowflake other)
        => other is null ? 1 : this.Value.CompareTo(other.Value);

    public static bool operator ==(Snowflake left, Snowflake right)
        => left is not null & right is not null && left.Value == right.Value;

    public static bool operator !=(Snowflake left, Snowflake right)
        => left.Value != right.Value;

    public static bool operator <(Snowflake left, Snowflake right)
        => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(Snowflake left, Snowflake right)
        => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(Snowflake left, Snowflake right)
        => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(Snowflake left, Snowflake right)
        => left is null ? right is null : left.CompareTo(right) >= 0;

    public static implicit operator Snowflake(ulong value)
        => new(value);

    public static implicit operator ulong(Snowflake snowflake)
        => snowflake.Value;

    public static implicit operator string(Snowflake snowflake)
        => snowflake.Value.ToString();

    public static implicit operator JsonValue(Snowflake snowflake)
        => JsonValue.Create<string>(snowflake.Value.ToString());
}
