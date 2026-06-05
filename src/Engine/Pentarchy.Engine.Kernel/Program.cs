using System;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;
using Pentarchy.Engine.Diagnostics.Core; // Pulling in our new diagnostic track!

namespace Pentarchy.Engine.Kernel;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("[SYSTEM] Running Saturated Memory Core Hex Audit");
        Console.WriteLine("=================================================\n");

        // 1. Initialize our Registry with 2 isolated memory pages
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 2);

        // 2. Map out our combat entity primitives across the page boundaries
        var simulationPayloads = new[]
        {
            (Page: 0, Slot: 0, Attr: new AttributeTuple("U1_PositionX", 42.0)),
            (Page: 0, Slot: 1, Attr: new AttributeTuple("U1_PositionY", 108.5)),
            (Page: 0, Slot: 2, Attr: new AttributeTuple("U1_Health",    100.0)),
            
            // Cross the boundary to Page 1
            (Page: 1, Slot: 0, Attr: new AttributeTuple("U2_PositionX", 500.2)),
            (Page: 1, Slot: 1, Attr: new AttributeTuple("U2_PositionY", 12.3)),
            (Page: 1, Slot: 2, Attr: new AttributeTuple("U2_Health",    85.0))
        };

        Console.WriteLine($"[STAGE 1] Streaming {simulationPayloads.Length} attributes into raw memory blocks...");
        foreach (var payload in simulationPayloads)
        {
            registry.CommitAttribute(payload.Page, payload.Slot, payload.Attr);
        }
        Console.WriteLine("  -> Materialization complete.\n");

        // 3. Hack directly into the WorldStateRegistry's internal page array 
        Console.WriteLine("[STAGE 2] Printing Intrusion Telemetry via MemoryPageVisualizer:\n");
        
        // For our test, we will instantiate individual pages to inspect the visualizer output explicitly!
        MemoryPage debugPage0 = new MemoryPage(0, 1);
        MemoryPage debugPage1 = new MemoryPage(1, 2);
        
        // Allocate our 32-byte scratchpad EXACTLY ONCE outside the loop execution frame
        Span<byte> slotBuffer = stackalloc byte[32];

        // Let's deliberately push a payload deep into Slot 25 (Offset 16 + 25 * 32 = byte 816)
        int deepSlot = 25;
        int targetOffsetDeep = 16 + (deepSlot * 32); // 816
        System.Text.Encoding.UTF8.GetBytes("DEEP_CORE_DATA").AsSpan().CopyTo(slotBuffer.Slice(0, 24));
        BitConverter.GetBytes(777.7).CopyTo(slotBuffer.Slice(24, 8));
        debugPage0.Write(targetOffsetDeep, slotBuffer);

        // Re-populating standalone pages directly to match our registry layout for explicit visual inspection
        foreach (var payload in simulationPayloads)
        {
            int targetOffset = 16 + (payload.Slot * 32);
            
            // Clean out the scratchpad from the previous iteration so data doesn't leak
            slotBuffer.Clear();
            
            // Serialize the current entity tuple payload into our stationary workbench
            System.Text.Encoding.UTF8.GetBytes(payload.Attr.Key).AsSpan().CopyTo(slotBuffer.Slice(0, 24));
            BitConverter.GetBytes(payload.Attr.Value).CopyTo(slotBuffer.Slice(24, 8));
            
            if (payload.Page == 0) debugPage0.Write(targetOffset, slotBuffer);
            else debugPage1.Write(targetOffset, slotBuffer);
        }

        // Render Page 0 Dump
        Console.WriteLine("--- CORE STORAGE SECTOR: PAGE 00 ---");
        string dump0 = MemoryPageVisualizer.RenderPageDump(debugPage0, bytesToScan: 128);
        Console.WriteLine(dump0);

        // Render Page 1 Dump
        Console.WriteLine("\n--- CORE STORAGE SECTOR: PAGE 01 ---");
        string dump1 = MemoryPageVisualizer.RenderPageDump(debugPage1, bytesToScan: 128);
        Console.WriteLine(dump1);

        // Render Page 0 Dump using our brand-new target window filter parameters!
        // We skip the first 800 bytes entirely and look strictly at the 64 bytes containing our deep slot.
        Console.WriteLine($"--- PROBING CORE STORAGE WINDOW: STARTING AT BYTE {targetOffsetDeep} ---");
        string dump2 = MemoryPageVisualizer.RenderPageDump(debugPage0, offsetStart: targetOffsetDeep, bytesToScan: 64);
        Console.WriteLine(dump2);

        // NEW: STAGE 2.5 - Invoking our advanced structural helper lenses
        Console.WriteLine("\n[STAGE 2.5] Invoking Advanced Structural Helper Lenses:\n");

        // 1. Check the allocation status grid for Page 0
        Console.WriteLine("--- VISUALIZING ENGINE SLOT DISTRIBUTION (PAGE 00) ---");
        string allocationGrid = MemoryPageVisualizer.RenderAllocationGrid(debugPage0, slotsToScan: 6);
        Console.WriteLine(allocationGrid);

        // Check the targeted allocation status grid for our deep memory partition
        Console.WriteLine("--- VISUALIZING TARGETED ENGINE SLOT DISTRIBUTION ---");
        allocationGrid = MemoryPageVisualizer.RenderAllocationGrid(debugPage0, startSlot: 22, slotsToScan: 5);
        Console.WriteLine(allocationGrid);

        // 2. Perform an atomic bit-level deconstruction of Unit 2's fractional PositionX variable
        Console.WriteLine("--- INSPECTING THE FLOATING POINT CORE MATRIS (U2_PositionX) ---");
        string ieeeDeconstruction = MemoryPageVisualizer.InspectIEEE754Double(registry.FetchAttribute(1, 0).Value);
        Console.WriteLine(ieeeDeconstruction);

        Console.WriteLine("[STAGE 3] Diagnostic Intrusion Scan Completed Successfully.");
    }
}