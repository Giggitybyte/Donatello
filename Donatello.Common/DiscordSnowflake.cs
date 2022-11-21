namespace Donatello;

using System;
using System.Globalization;

/// <summary></summary>
public class DiscordSnowflake : IComparable<DiscordSnowflake>
{
    private readonly static DateTimeOffset _discordEpoch = new(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <param name="rawValue">64-bit integer representing a Discord snowflake.</param>
    internal DiscordSnowflake(ulong rawValue)
    {
        this.CreationDate = DiscordEpoch.AddMilliseconds(rawValue >> 22);
        this.InternalWorkerId = (byte)((rawValue & 0x3E0000) >> 17);
        this.InternalProcessId = (byte)((rawValue & 0x1F000) >> 12);
        this.InternalIncrement = (ushort)(rawValue & 0xFFF);

        this.Value = rawValue;
    }

    /// <summary>The first second of the year 2015.</summary>
    public static DateTimeOffset DiscordEpoch => _discordEpoch;

    /// <summary>64-bit integer representation of this snowflake.</summary>
    public ulong Value { get; private init; }

    /// <summary>The date this snowflake was generated.</summary>
    public DateTimeOffset CreationDate { get; private init; }

    /// <summary>The internal worker's ID that was used by Discord to generate the snowflake.</summary>
    public byte InternalWorkerId { get; private init; }

    /// <summary>The internal process' ID that was used by Discord to generate the snowflake.</summary>
    public byte InternalProcessId { get; private init; }

    /// <summary>A number incremented by Discord every time a snowflake is generated.</summary>
    public ushort InternalIncrement { get; private init; }

    /// <summary>Returns the 64-bit integer representation of this snowflake as a string.</summary>
    public override string ToString()
        => this.Value.ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public override bool Equals(object obj)
        => obj is DiscordSnowflake snowflake && snowflake == this;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.Value, this.CreationDate, this.InternalWorkerId, this.InternalProcessId, this.InternalIncrement);

    /// <inheritdoc/>
    public int CompareTo(DiscordSnowflake other)
        => other is null ? 1 : this.Value.CompareTo(other.Value);

    public static bool operator ==(DiscordSnowflake left, DiscordSnowflake right)
        => left is not null & right is not null && left.Value == right.Value;

    public static bool operator !=(DiscordSnowflake left, DiscordSnowflake right)
        => left.Value != right.Value;

    public static bool operator <(DiscordSnowflake left, DiscordSnowflake right)
        => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(DiscordSnowflake left, DiscordSnowflake right)
        => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(DiscordSnowflake left, DiscordSnowflake right)
        => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(DiscordSnowflake left, DiscordSnowflake right)
        => left is null ? right is null : left.CompareTo(right) >= 0;

    public static implicit operator ulong(DiscordSnowflake snowflake)
        => snowflake.Value;

    public static implicit operator DiscordSnowflake(ulong value)
        => new(value);
}
