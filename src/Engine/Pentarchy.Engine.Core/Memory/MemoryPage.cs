using System;

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
    private readonly byte[] _rawBuffer;

    /// <summary>
    /// Gets a read-only view of the raw memory buffer.
    /// </summary>
    public ReadOnlySpan<byte> Buffer => _rawBuffer;

    public MemoryPage(Guid pageId)
    {
        _rawBuffer = new byte[PageSize];
        
        // Write the unique Page ID into the first 16 bytes of the page header
        byte[] idBytes = pageId.ToByteArray();
        idBytes.AsSpan().CopyTo(_rawBuffer.AsSpan(0, 16));
    }

    /// <summary>
    /// Writes a contiguous chunk of bytes directly into a specific offset of the page.
    /// </summary>
    public void Write(int offset, ReadOnlySpan<byte> data)
    {
        // Safety Bound Check: Prevent memory corruption or out-of-bounds exploits
        if (offset < 16 || offset + data.Length > PageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Memory write operation violated page boundaries.");
        }

        // Copy the raw bytes directly onto the binary canvas
        data.CopyTo(_rawBuffer.AsSpan(offset));
    }

    /// <summary>
    /// Slices a contiguous view of bytes out of the page without creating object copies.
    /// </summary>
    public ReadOnlySpan<byte> Read(int offset, int length)
    {
        // Safety Bound Check
        if (offset < 0 || offset + length > PageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Memory read operation violated page boundaries.");
        }

        // Return a zero-allocation window/slice of the buffer
        return _rawBuffer.AsSpan(offset, length);
    }
}