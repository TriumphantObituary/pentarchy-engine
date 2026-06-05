using System;
using System.Buffers.Binary;

namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// Represents the relational network atom of the engine. Encodes a topological bridge, 
/// traversal friction cost, and environmental configuration bitmasks into a 32-byte slot.
/// </summary>
/// <remarks>
/// DESIGN JUSTIFICATION:
/// This primitive uses explicit trailing zero-padding to naturally snap the total footprint 
/// to 32 bytes. This guarantees that when arrays of edges are laid out sequentially in page memory, 
/// every single 64-bit value aligns perfectly to a physical 8-byte boundaries. This eliminates 
/// CPU misaligned-access performance penalties.
/// </remarks>
public readonly struct GraphEdge : IEquatable<GraphEdge>
{
    public const int SizeInBytes = 32;

    public ulong SourceNodeId { get; }
    public ulong DestinationNodeId { get; }
    public double TraversalCost { get; }
    public uint Flags { get; }

    public GraphEdge(ulong sourceNodeId, ulong destinationNodeId, double traversalCost, uint flags = 0)
    {
        SourceNodeId = sourceNodeId;
        DestinationNodeId = destinationNodeId;
        TraversalCost = traversalCost;
        Flags = flags;
    }

    /// <summary>
    /// Flattens the graph edge into a pre-allocated 32-byte memory span destination window.
    /// </summary>
    public void Serialize(Span<byte> destination)
    {
        if (destination.Length < SizeInBytes)
        {
            throw new ArgumentException($"Serialization destination requires at least {SizeInBytes} bytes.", nameof(destination));
        }

        // Bytes 0-7: Source Node Handle
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(0, 8), SourceNodeId);

        // Bytes 8-15: Destination Node Handle
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8, 8), DestinationNodeId);

        // Bytes 16-23: Traversal Cost via IEEE 754 float binary conversions
        long costBits = BitConverter.DoubleToInt64Bits(TraversalCost);
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(16, 8), costBits);

        // Bytes 24-27: Contextual state flag bitfield ledger
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(24, 4), Flags);

        // Bytes 28-31: Zero out trailing padding explicitly to prevent leaking random memory states
        destination.Slice(28, 4).Clear();
    }

    /// <summary>
    /// Rehydrates a fixed 32-byte memory window back into a valid logical GraphEdge primitive.
    /// </summary>
    public static GraphEdge Deserialize(ReadOnlySpan<byte> slotData)
    {
        if (slotData.Length != SizeInBytes)
        {
            throw new ArgumentException($"Deserialization requires an exact {SizeInBytes}-byte window slot.", nameof(slotData));
        }

        ulong source = BinaryPrimitives.ReadUInt64LittleEndian(slotData.Slice(0, 8));
        ulong dest = BinaryPrimitives.ReadUInt64LittleEndian(slotData.Slice(8, 8));
        
        long costBits = BinaryPrimitives.ReadInt64LittleEndian(slotData.Slice(16, 8));
        double cost = BitConverter.Int64BitsToDouble(costBits);

        uint flags = BinaryPrimitives.ReadUInt32LittleEndian(slotData.Slice(24, 4));

        return new GraphEdge(source, dest, cost, flags);
    }

    // Unboxed value equality contracts maximizing execution velocity during graph traversal evaluations
    public bool Equals(GraphEdge other) =>
        SourceNodeId == other.SourceNodeId &&
        DestinationNodeId == other.DestinationNodeId &&
        TraversalCost.Equals(other.TraversalCost) &&
        Flags == other.Flags;

    public override bool Equals(object? obj) => obj is GraphEdge other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(SourceNodeId, DestinationNodeId, TraversalCost, Flags);
    public static bool operator ==(GraphEdge left, GraphEdge right) => left.Equals(right);
    public static bool operator !=(GraphEdge left, GraphEdge right) => !left.Equals(right);
}