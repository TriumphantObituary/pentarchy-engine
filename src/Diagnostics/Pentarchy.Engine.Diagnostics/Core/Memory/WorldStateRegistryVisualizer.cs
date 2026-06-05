using System;
using System.Text;
using Pentarchy.Engine.Core.Memory;

namespace Pentarchy.Engine.Diagnostics.Memory;

/// <summary>
/// Provides high-level schematic diagnostics and visual topology dashboards for the global WorldStateRegistry.
/// </summary>
public static class WorldStateRegistryVisualizer
{
    /// <summary>
    /// Renders a comprehensive system topography map of all allocated pages, occupancy densities, and layout track metrics.
    /// </summary>
    public static string RenderRegistryDashboard(WorldStateRegistry registry, int totalPages)
    {
        StringBuilder sb = new();
        sb.AppendLine("=============================================================================");
        sb.AppendLine("   PENTARCHY RUNTIME CORE REGISTRY SCHEMATIC MAP");
        sb.AppendLine("=============================================================================");
        sb.AppendLine($"  TOTAL MANAGED SPACE : {totalPages * MemoryPage.PageSize} Bytes ({totalPages} Pages × 1024B)");
        sb.AppendLine("  PAGE DISTRIBUTION TOPOLOGY:");
        sb.AppendLine("  ───────────────────────────────────────────────────────────────────────────");

        for (int i = 0; i < totalPages; i++)
        {
            ulong ownerId = registry.GetEntityOwnerOfPage(i);
            
            // Determine structural layout tags
            string modeLabel = "UNASSIGNED";
            int activeSlots = 0;

            // Probe allocations inside our slots to calculate density metrics safely
            for (int slot = 0; slot < MemoryPage.MaxSlots; slot++)
            {
                if (IsSlotOccupied(registry, i, slot, out string detectedMode))
                {
                    modeLabel = detectedMode;
                    activeSlots++;
                }
            }

            double activePercentage = ((double)activeSlots / MemoryPage.MaxSlots) * 100.0;
            string barGraph = DrawMicroStatusBar(activeSlots, MemoryPage.MaxSlots);

            sb.AppendLine($"  [PAGE {i:D2}] ── [{modeLabel,-10}] ── OWNER ENTITY: {ownerId,-6}");
            sb.AppendLine($"          Allocation Density: {barGraph} {activeSlots:D2}/{MemoryPage.MaxSlots} slots ({activePercentage:F1}%)");
            sb.AppendLine("  ───────────────────────────────────────────────────────────────────────────");
        }

        return sb.ToString();
    }

    private static bool IsSlotOccupied(WorldStateRegistry registry, int page, int slot, out string modeLabel)
    {
        modeLabel = "VACANT";
        try
        {
            _ = registry.FetchAttribute(page, slot);
            modeLabel = "ATTRIBUTES";
            return true;
        }
        catch (InvalidOperationException)
        {
            try
            {
                _ = registry.FetchTag(page, slot);
                modeLabel = "TAGS      ";
                return true;
            }
            catch (InvalidOperationException)
            {
                try
                {
                    _ = registry.FetchEdge(page, slot);
                    modeLabel = "GRAPH EDGES";
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }
    }

    private static string DrawMicroStatusBar(int active, int max)
    {
        int totalBlocks = 15;
        int filledBlocks = (int)Math.Round((double)active / max * totalBlocks);
        
        return "[" + new string('■', filledBlocks) + new string('·', totalBlocks - filledBlocks) + "]";
    }
}