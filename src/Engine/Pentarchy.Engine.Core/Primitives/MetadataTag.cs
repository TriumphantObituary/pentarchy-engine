using System;
using System.Text;

namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// Represents the qualitative, categorical atom of the engine. Serializes short string 
/// identifiers directly into an in-line, fixed 32-byte memory slot.
/// </summary>
/// <remarks>
/// DESIGN JUSTIFICATION:
/// This primitive completely eliminates internal byte-array references to ensure the struct 
/// is purely stack-allocated. Text is serialized directly into pre-allocated memory buffers on demand, 
/// bypassing the .NET managed heap and preventing high-frequency GC collection spikes.
/// </remarks>
public readonly struct MetadataTag : IEquatable<MetadataTag>
{
    public const int MaxTagSize = 32;

    public string Value { get; }

    public MetadataTag(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Metadata tag value cannot be null.");
        }

        // Enforce raw UTF-8 byte boundary checking to handle multi-byte Unicode characters safely
        if (Encoding.UTF8.GetByteCount(value) > MaxTagSize)
        {
            throw new ArgumentException($"Metadata tag exceeds the strict structural {MaxTagSize}-byte slot limit.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Flattens the qualitative string payload directly into a pre-allocated 32-byte destination span window.
    /// </summary>
    public void Serialize(Span<byte> destination)
    {
        if (destination.Length < MaxTagSize)
        {
            throw new ArgumentException($"Serialization destination requires at least {MaxTagSize} bytes.", nameof(destination));
        }

        // 1. Clear the destination window to guarantee a clean slate of 0x00 null-padding bytes
        destination.Slice(0, MaxTagSize).Clear();

        // 2. Convert and write the text string directly into the start of the buffer slice
        Encoding.UTF8.GetBytes(Value, destination);
    }

    /// <summary>
    /// Rehydrates a fixed 32-byte memory window back into a valid logical MetadataTag primitive.
    /// </summary>
    public static MetadataTag Deserialize(ReadOnlySpan<byte> slotData)
    {
        if (slotData.Length != MaxTagSize)
        {
            throw new ArgumentException($"Deserialization requires an exact {MaxTagSize}-byte window slot.", nameof(slotData));
        }

        // Scan the binary window to locate the trailing null-terminator padding boundary
        int actualLength = 0;
        while (actualLength < MaxTagSize && slotData[actualLength] != 0x00)
        {
            actualLength++;
        }

        // Extract the active characters and rehydrate the logical string identifier
        string rehydratedValue = Encoding.UTF8.GetString(slotData.Slice(0, actualLength));
        return new MetadataTag(rehydratedValue);
    }

    // High-performance value string comparison bypassing object boxing mechanics
    public bool Equals(MetadataTag other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is MetadataTag other && Equals(other);
    public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
    public static bool operator ==(MetadataTag left, MetadataTag right) => left.Equals(right);
    public static bool operator !=(MetadataTag left, MetadataTag right) => !left.Equals(right);
}