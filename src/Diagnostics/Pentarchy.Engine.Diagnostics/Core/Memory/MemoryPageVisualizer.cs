using System;
using System.Text;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Diagnostics.Memory;

/// <summary>
/// Renders low-level visualization layouts directly out of a 1KB MemoryPage,
/// exposing raw hexadecimal matrices and localized slot allocation grids.
/// </summary>
public static class MemoryPageVisualizer
{
    public static string RenderPageDump(MemoryPage page, int offsetStart = 0, int bytesToScan = 128)
    {
        if (offsetStart < 0 || offsetStart >= MemoryPage.PageSize) offsetStart = 0;
        if (offsetStart + bytesToScan > MemoryPage.PageSize) bytesToScan = MemoryPage.PageSize - offsetStart;

        int alignedStart = (offsetStart / 16) * 16;
        int alignedLength = bytesToScan + (offsetStart - alignedStart);
        if (alignedStart + alignedLength > MemoryPage.PageSize) alignedLength = MemoryPage.PageSize - alignedStart;

        ReadOnlySpan<byte> pageBuffer = page.Buffer.Span;
        StringBuilder sb = new();

        sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
        sb.AppendLine($"  │ HEX DUMP ── TRACKER: {page.PageTrackerId:D3} │ OWNER: {page.EntityId,-13} │ MODE: {page.PageTypeMode}                   │");
        sb.AppendLine("  ├─────────────────────────────────────────────────────────────────────────────┤");
        sb.AppendLine("  │ OFFSET    00 10 20 30 40 50 60 70  80 90 A0 B0 C0 D0 E0 F0  ASCII           │");
        sb.AppendLine("  ├─────────────────────────────────────────────────────────────────────────────┤");

        for (int row = alignedStart; row < alignedStart + alignedLength; row += 16)
        {
            sb.Append($"  │ {row:X8}  ");
            int rowRemainder = Math.Min(16, (alignedStart + alignedLength) - row);

            for (int i = 0; i < 16; i++)
            {
                if (i == 8) sb.Append(" ");
                sb.Append(i < rowRemainder ? $"{pageBuffer[row + i]:X2} " : "   ");
            }
            sb.Append(" ");

            // Text representation column matrix
            for (int i = 0; i < rowRemainder; i++)
            {
                byte b = pageBuffer[row + i];
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            int missingBytes = 16 - rowRemainder;
            sb.AppendLine(new string(' ', missingBytes) + "│");
        }

        sb.AppendLine("  └─────────────────────────────────────────────────────────────────────────────┘");
        return sb.ToString();
    }

    public static string RenderPageAllocationGrid(WorldStateRegistry registry, int pageIndex)
    {
        StringBuilder sb = new();
        ulong owner = registry.GetEntityOwnerOfPage(pageIndex);
        
        sb.AppendLine($"  ┌─── PAGE ALLOCATION TRACKER [INDEX: {pageIndex:D2}] ── OWNER: {owner}");
        
        for (int slot = 0; slot < MemoryPage.MaxSlots; slot++)
        {
            string slotStatus = TryFormatSlotData(registry, pageIndex, slot);
            
            // Draw visual tree layout connections
            string prefix = (slot == MemoryPage.MaxSlots - 1) ? "  └── " : "  ├── ";
            sb.AppendLine($"{prefix}Slot {slot:D2}: {slotStatus}");
        }
        return sb.ToString();
    }

    private static string TryFormatSlotData(WorldStateRegistry registry, int pageIndex, int slot)
    {
        try
        {
            AttributeTuple attr = registry.FetchAttribute(pageIndex, slot);
            return $"[ATTR] '{attr.Key}' ──> {attr.Value}";
        }
        catch (InvalidOperationException)
        {
            try
            {
                MetadataTag tag = registry.FetchTag(pageIndex, slot);
                return $"[TAG ] \"{tag.Value}\"";
            }
            catch (InvalidOperationException)
            {
                try
                {
                    GraphEdge edge = registry.FetchEdge(pageIndex, slot);
                    return $"[EDGE] Node({edge.SourceNodeId}) ──({edge.TraversalCost})──> Node({edge.DestinationNodeId})";
                }
                catch (InvalidOperationException)
                {
                    return "[VACANT]";
                }
            }
        }
    }
}