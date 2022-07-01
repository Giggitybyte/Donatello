namespace Donatello.Entity;

using System;
using System.Globalization;

// Modified from https://github.com/DSharpPlus/DSharpPlus/blob/1dc09e6c058868f6df8d11607336a91f60cb87da/DSharpPlus.Core/RestEntities/DiscordSnowflake.cs
// Licensed LGPL 3.0 | https://www.gnu.org/licenses/lgpl-3.0.txt
public sealed class DiscordSnowflake : IComparable<DiscordSnowflake>
{
    private readonly static DateTimeOffset _discordEpoch = new(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <param name="rawValue">64-bit integer representing a Discord snowflake.</param>
    internal DiscordSnowflake(ulong rawValue)
    {
        this.Timestamp = DiscordEpoch.AddMilliseconds(rawValue >> 22);
        this.InternalWorkerId = (byte)((rawValue & 0x3E0000) >> 17);
        this.InternalProcessId = (byte)((rawValue & 0x1F000) >> 12);
        this.InternalIncrement = (ushort)(rawValue & 0xFFF);

        this.Value = rawValue;
    }

    /// <summary>The first second of the year 2015.</summary>
    public static DateTimeOffset DiscordEpoch => _discordEpoch;

    /// <summary>64-bit integer representation of this snowflake.</summary>
    public ulong Value { get; init; }

    /// <summary>Time since the Discord epoch.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>The internal worker's ID that was used to generate the snowflake.</summary>
    public byte InternalWorkerId { get; init; }

    /// <summary>The internal process' ID that was used to generate the snowflake.</summary>
    public byte InternalProcessId { get; init; }

    /// <summary>A number incremented by 1 every time the snowflake is generated.</summary>
    public ushort InternalIncrement { get; init; }

    /// <summary>Returns the 64-bit integer representation of this snowflake as a string.</summary>
    public override string ToString()
        => this.Value.ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.Value, this.Timestamp, this.InternalWorkerId, this.InternalProcessId, this.InternalIncrement);

    /// <inheritdoc/>
    public int CompareTo(DiscordSnowflake other)
        => other is null ? 1 : this.Value.CompareTo(other.Value);

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
