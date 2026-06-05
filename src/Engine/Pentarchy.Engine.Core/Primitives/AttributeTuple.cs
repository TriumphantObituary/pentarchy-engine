using System;
using System.Buffers.Binary;
using System.Text;

namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// Represents the quantitative atom of the engine. Packs a 24-character description 
/// and an 8-byte floating-point number into an exact, immutable 32-byte hardware footprint.
/// </summary>
/// <remarks>
/// DESIGN JUSTIFICATION:
/// Convert this to a 'readonly struct' to avoid managed heap allocations, pointer-chasing, 
/// and Garbage Collection pressure. This structure guarantees that numbers are kept adjacent to 
/// their keys in memory, maximizing CPU L1/L2 cache locality during intensive systems loops.
/// </remarks>
public readonly struct AttributeTuple : IEquatable<AttributeTuple>
{
    public const int SizeInBytes = 32;
    private const int MaxKeyBytes = 24;

    public string Key { get; }
    public double Value { get; }

    public AttributeTuple(string key, double value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Attribute key cannot be null.");
        }

        // Validate raw byte length rather than character count to safely accommodate multi-byte UTF-8 symbols
        if (Encoding.UTF8.GetByteCount(key) > MaxKeyBytes)
        {
            throw new ArgumentException($"Key exceeds the strict structural {MaxKeyBytes}-byte storage ceiling.", nameof(key));
        }

        Key = key;
        Value = value;
    }

    /// <summary>
    /// Flattens the quantitative primitive directly into a pre-allocated 32-byte destination span window.
    /// </summary>
    public void Serialize(Span<byte> destination)
    {
        if (destination.Length < SizeInBytes)
        {
            throw new ArgumentException($"Serialization destination requires at least {SizeInBytes} bytes.", nameof(destination));
        }

        // 1. Clear the 32-byte slot segment to prevent random bit-bleed anomalies
        destination.Slice(0, SizeInBytes).Clear();

        // 2. Bytes 0-23: Serialize the key text into the front of the window
        Encoding.UTF8.GetBytes(Key, destination.Slice(0, MaxKeyBytes));

        // 3. Bytes 24-31: Write the 8-byte float value at the back of the window using IEEE 754 bit-conversion
        long valueBits = BitConverter.DoubleToInt64Bits(Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(MaxKeyBytes, 8), valueBits);
    }

    /// <summary>
    /// Rehydrates a fixed 32-byte memory window back into a valid logical AttributeTuple primitive.
    /// </summary>
    public static AttributeTuple Deserialize(ReadOnlySpan<byte> slotData)
    {
        if (slotData.Length != SizeInBytes)
        {
            throw new ArgumentException($"Deserialization requires an exact {SizeInBytes}-byte window slot.", nameof(slotData));
        }

        // Locate the text boundary by scanning for the trailing null-terminator padding (0x00)
        int keyByteLength = 0;
        while (keyByteLength < MaxKeyBytes && slotData[keyByteLength] != 0x00)
        {
            keyByteLength++;
        }

        string key = Encoding.UTF8.GetString(slotData.Slice(0, keyByteLength));

        // Read the binary bits directly from the trailing 8-byte track
        long valueBits = BinaryPrimitives.ReadInt64LittleEndian(slotData.Slice(MaxKeyBytes, 8));
        double value = BitConverter.Int64BitsToDouble(valueBits);

        return new AttributeTuple(key, value);
    }

    // High-performance value equality overrides to eliminate object boxing overhead
    public bool Equals(AttributeTuple other) => Key == other.Key && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is AttributeTuple other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Key, Value);
    public static bool operator ==(AttributeTuple left, AttributeTuple right) => left.Equals(right);
    public static bool operator !=(AttributeTuple left, AttributeTuple right) => !left.Equals(right);
}