using System;
using System.Text;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Memory;

/// <summary>
/// The central coordinator that manages segmented memory pages and translates 
/// abstract data primitives into precise, contiguous binary offsets.
/// </summary>
public sealed class WorldStateRegistry
{
    private readonly MemoryPage[] _pages;
    
    // Fixed layout dimensions for our prototype
    private const int HeaderSize = 16;
    private const int SlotSize = 32;
    private const int ValueSize = 8;
    private const int KeySize = SlotSize - ValueSize; // 24 Bytes

    public WorldStateRegistry(int pageCount)
    {
        _pages = new MemoryPage[pageCount];
        for (int i = 0; i < pageCount; i++)
        {
            _pages[i] = new MemoryPage(Guid.NewGuid());
        }
    }

    /// <summary>
    /// Commits an abstract AttributeTuple primitive into a specific page and slot address.
    /// </summary>
    public void CommitAttribute(int pageIndex, int slotIndex, AttributeTuple attribute)
    {
        // Explicitly safeguard page boundaries relative to our allocated array size
        if (pageIndex < 0 || pageIndex >= _pages.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index must be between 0 and {_pages.Length - 1}.");
        }

        // Explicitly safeguard slot boundaries (0 to 31 slots maximum per 1KB page)
        if (slotIndex < 0 || slotIndex > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index must be between 0 and 31.");
        }

        MemoryPage page = _pages[pageIndex];
        int targetOffset = HeaderSize + (slotIndex * SlotSize);

        // 1. Serialize the String Key into a 24-byte binary window
        byte[] keyBytes = Encoding.UTF8.GetBytes(attribute.Key);
        Span<byte> slotBuffer = stackalloc byte[SlotSize];

        // Ensure the string isn't too long for our fixed primitive slot boundary
        int bytesToCopy = Math.Min(keyBytes.Length, KeySize);
        keyBytes.AsSpan(0, bytesToCopy).CopyTo(slotBuffer.Slice(0, KeySize));

        // 2. Serialize the Double Value into the final 8-byte binary window
        byte[] valueBytes = BitConverter.GetBytes(attribute.Value);
        valueBytes.CopyTo(slotBuffer.Slice(KeySize, ValueSize));

        // 3. Slap the fully packed 32-byte slot onto the raw contiguous memory page
        page.Write(targetOffset, slotBuffer);
    }

    /// <summary>
    /// Fetches an abstract AttributeTuple directly from raw binary page offsets.
    /// </summary>
    public AttributeTuple FetchAttribute(int pageIndex, int slotIndex)
    {
        // Explicitly safeguard page boundaries relative to our allocated array size
        if (pageIndex < 0 || pageIndex >= _pages.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index must be between 0 and {_pages.Length - 1}.");
        }

        // Explicitly safeguard slot boundaries (0 to 31 slots maximum per 1KB page)
        if (slotIndex < 0 || slotIndex > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index must be between 0 and 31.");
        }

        MemoryPage page = _pages[pageIndex];
        int targetOffset = HeaderSize + (slotIndex * SlotSize);

        // Read the exact 32-byte chunk out of the page window
        ReadOnlySpan<byte> slotData = page.Read(targetOffset, SlotSize);

        // 1. Rehydrate the Key String (trimming off empty trailing null bytes)
        ReadOnlySpan<byte> keyWindow = slotData.Slice(0, KeySize);
        int actualLength = 0;
        while (actualLength < KeySize && keyWindow[actualLength] != 0)
        {
            actualLength++;
        }
        string key = Encoding.UTF8.GetString(keyWindow.Slice(0, actualLength));

        // 2. Rehydrate the Double Value from the last 8 bytes
        double value = BitConverter.ToDouble(slotData.Slice(KeySize, ValueSize));

        return new AttributeTuple(key, value);
    }
}