using System;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;
using Pentarchy.Engine.Diagnostics; // Pulling in our new diagnostic track!

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
        // using our modern diagnostic intrusion tools to see the raw byte canvas.
        // We will look at the first 128 bytes of each page (Header + 3 Data Slots).
        Console.WriteLine("[STAGE 2] Printing Intrusion Telemetry via HexDumpVisualizer:\n");
        
        // Since our registry stores pages inside an array, let's extract them via a bypass helper method
        // or temporary simulation wrapper to view the data blocks directly.
        // For our test, we will instantiate individual pages to inspect the visualizer output explicitly!
        
        MemoryPage debugPage0 = new MemoryPage(Guid.NewGuid());
        MemoryPage debugPage1 = new MemoryPage(Guid.NewGuid());
        
        // Allocate our 32-byte scratchpad EXACTLY ONCE outside the loop execution frame
        Span<byte> slotBuffer = stackalloc byte[32];

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
        string dump0 = HexDumpVisualizer.RenderPageDump(debugPage0, bytesToScan: 128);
        Console.WriteLine(dump0);

        // Render Page 1 Dump
        Console.WriteLine("\n--- CORE STORAGE SECTOR: PAGE 01 ---");
        string dump1 = HexDumpVisualizer.RenderPageDump(debugPage1, bytesToScan: 128);
        Console.WriteLine(dump1);

        Console.WriteLine("[STAGE 3] Diagnostic Intrusion Scan Completed Successfully.");
    }
}