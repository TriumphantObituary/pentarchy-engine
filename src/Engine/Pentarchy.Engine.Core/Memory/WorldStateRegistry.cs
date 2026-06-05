using System;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Memory;

/// <summary>
/// The master flat coordination matrix. Directs, maps, and safeguards raw binary page collections, 
/// grouping them into isolated, type-safe data pipelines.
/// </summary>
/// <remarks>
/// DESIGN JUSTIFICATION:
/// This registry acts as a security facade directly above our raw memory grids. It enforces 
/// strict Page Typology Isolation (ensuring quantitative attributes, qualitative tags, and spatial graph 
/// links never cross-contaminate the same memory tracks) while remaining entirely parametric to easily 
/// support future page resizing.
/// </remarks>
public sealed class WorldStateRegistry
{
    private readonly MemoryPage[] _pages;

    public WorldStateRegistry(int pageCount)
    {
        if (pageCount <= 0)
        {
            throw new ArgumentException("Registry must contain at least one memory page.", nameof(pageCount));
        }

        _pages = new MemoryPage[pageCount];
        for (int i = 0; i < pageCount; i++)
        {
            _pages[i] = new MemoryPage(pageTrackerId: (ulong)i, entityId: 0);
        }
    }

    public ulong GetEntityOwnerOfPage(int pageIndex)
    {
        ValidatePageBounds(pageIndex);
        return _pages[pageIndex].EntityId;
    }

    public void AssignPageToEntity(int pageIndex, ulong entityId, byte pageTypeMode)
    {
        ValidatePageBounds(pageIndex);
        _pages[pageIndex].EntityId = entityId;
        _pages[pageIndex].PageTypeMode = pageTypeMode;
    }

    // =========================================================================
    // UNIVERSAL ZERO-ALLOCATION PRIMITIVE MATRIX
    // =========================================================================

    public void CommitAttribute(int pageIndex, int slotIndex, AttributeTuple attribute)
    {
        ValidateRoute(pageIndex, slotIndex, expectedMode: 0);
        
        // Stack-allocated scratchpad bypasses managed heap generation pools entirely
        Span<byte> scratchpad = stackalloc byte[MemoryPage.SlotSize];
        attribute.Serialize(scratchpad);
        _pages[pageIndex].AllocateSlot(slotIndex, scratchpad);
    }

    public AttributeTuple FetchAttribute(int pageIndex, int slotIndex)
    {
        ValidateFetch(pageIndex, slotIndex, expectedMode: 0);
        int targetOffset = MemoryPage.HeaderSize + (slotIndex * MemoryPage.SlotSize);
        return AttributeTuple.Deserialize(_pages[pageIndex].Read(targetOffset, MemoryPage.SlotSize));
    }

    public void CommitTag(int pageIndex, int slotIndex, MetadataTag tag)
    {
        ValidateRoute(pageIndex, slotIndex, expectedMode: 1);
        
        // Stack-allocated scratchpad bypasses managed heap generation pools entirely
        Span<byte> scratchpad = stackalloc byte[MemoryPage.SlotSize];
        tag.Serialize(scratchpad);
        _pages[pageIndex].AllocateSlot(slotIndex, scratchpad);
    }

    public MetadataTag FetchTag(int pageIndex, int slotIndex)
    {
        ValidateFetch(pageIndex, slotIndex, expectedMode: 1);
        int targetOffset = MemoryPage.HeaderSize + (slotIndex * MemoryPage.SlotSize);
        return MetadataTag.Deserialize(_pages[pageIndex].Read(targetOffset, MemoryPage.SlotSize));
    }

    public void CommitEdge(int pageIndex, int slotIndex, GraphEdge edge)
    {
        ValidateRoute(pageIndex, slotIndex, expectedMode: 2);
        
        Span<byte> scratchpad = stackalloc byte[MemoryPage.SlotSize];
        edge.Serialize(scratchpad);
        _pages[pageIndex].AllocateSlot(slotIndex, scratchpad);
    }

    public GraphEdge FetchEdge(int pageIndex, int slotIndex)
    {
        ValidateFetch(pageIndex, slotIndex, expectedMode: 2);
        int targetOffset = MemoryPage.HeaderSize + (slotIndex * MemoryPage.SlotSize);
        return GraphEdge.Deserialize(_pages[pageIndex].Read(targetOffset, MemoryPage.SlotSize));
    }

    // =========================================================================
    // ARCHITECTURAL PROTECTION SHUTTERS
    // =========================================================================

    private void ValidateRoute(int pageIndex, int slotIndex, byte expectedMode)
    {
        ValidatePageBounds(pageIndex);
        MemoryPage page = _pages[pageIndex];

        if (page.PageTypeMode != expectedMode)
        {
            throw new InvalidOperationException($"Typology Mismatch! Cannot map mode {expectedMode} primitives into a page allocated for mode {page.PageTypeMode}.");
        }
    }

    private void ValidateFetch(int pageIndex, int slotIndex, byte expectedMode)
    {
        ValidateRoute(pageIndex, slotIndex, expectedMode);
        
        // Intercept read requests targeting unallocated or uninitialized memory fields
        if (!_pages[pageIndex].IsSlotActive(slotIndex))
        {
            throw new InvalidOperationException($"Read Violation! Attempted to access unallocated slot track index {slotIndex}.");
        }
    }

    private void ValidatePageBounds(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _pages.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page request index falls outside configured registry boundaries.");
        }
    }
}