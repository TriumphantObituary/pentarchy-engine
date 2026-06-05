using System;
using System.Buffers.Binary;

namespace Pentarchy.Engine.Core.Memory;

/// <summary>
/// A fixed-size, contiguous block of raw binary memory representing a sector of world state.
/// This acts as the physical, allocation-free clay for our stateless modules.
/// </summary>
public sealed class MemoryPage
{
    // Our prototype page size is pinned to exactly 1024 bytes (1 KB)
    public const int PageSize = 1024;

    // The physical array allocated contiguously on the machine's memory layout
    private readonly byte[] _buffer = new byte[PageSize];
    public ReadOnlySpan<byte> Buffer => _buffer;

    // Exposed read-only properties that parse the raw header bytes on demand
    public ulong PageTrackerId => BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(0, 8));
    public ulong EntityId      => BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(8, 8));

    public MemoryPage(ulong pageTrackerId, ulong entityId)
    {
        // Step 1: Stamp the 8-byte Page Tracker ID into Bytes 0 to 7
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(0, 8), pageTrackerId);

        // Step 2: Stamp the 8-byte Entity ID Handle into Bytes 8 to 15
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(8, 8), entityId);
    }

    /// <summary>
    /// Writes a contiguous chunk of bytes directly into a specific offset of the page.
    /// </summary>
    public void Write(int offset, ReadOnlySpan<byte> data)
    {
        if (offset < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot write directly into the protected 16-byte header zone.");
        }

        if (offset + data.Length > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Write command exceeds physical 1KB page boundaries.");
        }

        // Copy the raw bytes directly onto the binary canvas
        data.CopyTo(_buffer.AsSpan(offset));
    }

    /// <summary>
    /// Slices a contiguous view of bytes out of the page without creating object copies.
    /// </summary>
    public ReadOnlySpan<byte> Read(int offset, int length)
    {
        if (offset < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read directly from the protected 16-byte header zone.");
        }

        if (offset + length > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Read command exceeds physical 1KB page boundaries.");
        }

        // Return a zero-allocation window/slice of the buffer
        return _buffer.AsSpan(offset, length);
    }
}