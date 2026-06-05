using System;
using System.Buffers.Binary;

namespace Pentarchy.Engine.Core.Memory;

/// <summary>
/// A fixed-size, contiguous block of raw binary memory representing a sector of world state.
/// This acts as the physical, allocation-free clay for our stateless engine systems.
/// </summary>
/// <remarks>
/// DESIGN JUSTIFICATION:
/// Uses a 64-byte header aligned perfectly to modern CPU cache-line boundaries.
/// By grouping tracking IDs, typology modes, and active slot bitmasks in the first 64 bytes, 
/// the processor can pre-fetch allocation metadata instantly in a single memory clock cycle.
/// </remarks>
public sealed class MemoryPage
{
    public const int PageSize = 1024; // 1KB Parametric Sandbox Page Size
    public const int HeaderSize = 64; // Cache-line aligned layout track
    public const int SlotSize = 32;   // Structural footprint for all primitives
    public const int MaxSlots = (PageSize - HeaderSize) / SlotSize;

    private readonly byte[] _buffer;

    public Memory<byte> Buffer => _buffer;

    // Bytes 00-07: Physical storage topology index tracked by the registry
    public ulong PageTrackerId
    {
        get => BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(0, 8));
        set => BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(0, 8), value);
    }
    
    // Bytes 08-15: Full 64-bit logical entity handle (Supports 18.4 quintillion handles)
    public ulong EntityId
    {
        get => BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(8, 8));
        set => BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(8, 8), value);
    }

    // Byte 16: Structural Typology Mode (0 = Attribute, 1 = Metadata, 2 = Graph)
    public byte PageTypeMode
    {
        get => _buffer[16];
        set => _buffer[16] = value;
    }

    // Bytes 17-20: Dense 32-bit allocation bitmask array tracking active (1) vs free (0) slots
    public uint AllocationMask
    {
        get => BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(17, 4));
        private set => BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(17, 4), value);
    }

    public MemoryPage(ulong pageTrackerId, ulong entityId, byte pageTypeMode = 0)
    {
        _buffer = new byte[PageSize];
        PageTrackerId = pageTrackerId;
        EntityId = entityId;
        PageTypeMode = pageTypeMode;
        AllocationMask = 0; // Initialize perfectly pristine
    }

    /// <summary>
    /// Commits a raw primitive payload to a specific slot and flips its allocation tracking bit to 1.
    /// </summary>
    public void AllocateSlot(int slotIndex, ReadOnlySpan<byte> data)
    {
        ValidateSlotIndex(slotIndex);
        int targetOffset = HeaderSize + (slotIndex * SlotSize);

        // Copy raw bits directly to calculated target offset slot bounds
        data.CopyTo(_buffer.AsSpan(targetOffset, SlotSize));

        // Flip tracking bit to active using bitwise OR
        AllocationMask |= (1U << slotIndex);
    }

    /// <summary>
    /// Flips an allocation tracking bit back to 0, immediately reclaiming the slot space.
    /// </summary>
    public void FreeSlot(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        
        // Clear the allocation tracking bit back to 0 using bitwise AND NOT
        AllocationMask &= ~(1U << slotIndex);
    }

    /// <summary>
    /// Checks the header allocation ledger to verify if a slot contains active data.
    /// </summary>
    public bool IsSlotActive(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        return (AllocationMask & (1U << slotIndex)) != 0;
    }

    /// <summary>
    /// Writes a contiguous chunk of bytes directly into an explicit, bounded layout offset.
    /// </summary>
    public void Write(int offset, ReadOnlySpan<byte> data)
    {
        if (offset < HeaderSize || offset + data.Length > PageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Write operation violates physical page space guardrails.");
        }

        // Bound destination span to the exact incoming length to protect neighboring layout segments
        data.CopyTo(_buffer.AsSpan(offset, data.Length));
    }

    /// <summary>
    /// Slices a contiguous view of bytes out of the page without creating managed object allocations.
    /// </summary>
    public ReadOnlySpan<byte> Read(int offset, int length)
    {
        if (offset < HeaderSize || offset + length > PageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Read operation violates physical page space guardrails.");
        }

        return _buffer.AsSpan(offset, length);
    }

    private void ValidateSlotIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index must be between 0 and {MaxSlots - 1}.");
        }
    }
}