using System;
using System.Text;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Kernel;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("[SYSTEM] Scaling Pentarchy Memory Saturation Test");
        Console.WriteLine("=================================================\n");

        // Initialize our Registry with 2 isolated memory pages
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 2);

        // 1. Generate a bulk batch of attributes to stress our offsets
        // We will mock data for two separate game entities: "Unit_01" and "Unit_02"
        var dataPayloads = new[]
        {
            (Page: 0, Slot: 0, Attr: new AttributeTuple("U1_PositionX", 42.0)),
            (Page: 0, Slot: 1, Attr: new AttributeTuple("U1_PositionY", 108.5)),
            (Page: 0, Slot: 2, Attr: new AttributeTuple("U1_Health",    100.0)),
            (Page: 0, Slot: 3, Attr: new AttributeTuple("U1_MaxDamage", 250.75)),
            
            // Now let's cross the page boundary over to Page 1!
            (Page: 1, Slot: 0, Attr: new AttributeTuple("U2_PositionX", 500.2)),
            (Page: 1, Slot: 1, Attr: new AttributeTuple("U2_PositionY", 12.3)),
            (Page: 1, Slot: 2, Attr: new AttributeTuple("U2_Health",    85.0)),
            (Page: 1, Slot: 3, Attr: new AttributeTuple("U2_MaxDamage", 400.0))
        };

        Console.WriteLine($"[STAGE 1] Mass streaming {dataPayloads.Length} primitives into binary matrix...");
        foreach (var payload in dataPayloads)
        {
            registry.CommitAttribute(payload.Page, payload.Slot, payload.Attr);
        }
        Console.WriteLine("  -> Bulk ingestion complete. Matrix saturated.\n");

        // 2. Invoke our Telemetry Viewer on BOTH pages to inspect the layout
        Console.WriteLine("[STAGE 2] Printing Multi-Page Telemetry Memory Maps:");
        PrintMemoryPageMap(registry, pageIndex: 0, totalSlotsToPrint: 5);
        Console.WriteLine();
        PrintMemoryPageMap(registry, pageIndex: 1, totalSlotsToPrint: 5);

        // 3. Scale Rehydration Verification Loop
        Console.WriteLine("\n[STAGE 3] Executing batch cross-page rehydration verification...");
        bool allPassed = true;

        foreach (var payload in dataPayloads)
        {
            AttributeTuple recovered = registry.RehydrateAttribute(payload.Page, payload.Slot);
            
            // Check if the key and value match what we put in
            if (recovered.Key != payload.Attr.Key || Math.Abs(recovered.Value - payload.Attr.Value) > 0.001)
            {
                Console.WriteLine($"  [!] ERROR mismatch at Page {payload.Page}, Slot {payload.Slot}!");
                allPassed = false;
            }
        }

        Console.WriteLine("=================================================");
        Console.WriteLine("[VERIFICATION RESULTS]");
        if (allPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  -> STATUS: SUCCESS. Multi-Page Isolation & Boundary Scaling Verified.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  -> STATUS: CRITICAL FAILURE. Cross-page memory bleed detected.");
            Console.ResetColor();
        }
        Console.WriteLine("=================================================");
    }

    /// <summary>
    /// A rudimentary logging utility that renders a structured structural audit
    /// of a specific memory page's layout.
    /// </summary>
    private static void PrintMemoryPageMap(WorldStateRegistry registry, int pageIndex, int totalSlotsToPrint)
    {
        Console.WriteLine("-------------------------------------------------------------------------");
        Console.WriteLine($"| METADATA SECTOR: PAGE {pageIndex:00} BUFFER MAP                                 |");
        Console.WriteLine("-------------------------------------------------------------------------");
        
        Console.WriteLine("[0000-0015] [HEADER / PAGE ID GUID] -> LOCKED & PROTECTED");

        for (int slot = 0; slot < totalSlotsToPrint; slot++)
        {
            int startByte = 16 + (slot * 32);
            int endByte = startByte + 31;
            
            AttributeTuple attribute = registry.RehydrateAttribute(pageIndex, slot);
            Console.Write($"[{startByte:D4}-{endByte:D4}] [SLOT {slot:02} DATA REGISTRY] -> ");
            PrintSlotTelemetry(attribute);
        }
        
        Console.WriteLine("-------------------------------------------------------------------------");
    }

    private static void PrintSlotTelemetry(AttributeTuple attribute)
    {
        if (string.IsNullOrEmpty(attribute.Key))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[EMPTY REGISTRY SECTOR (0x00)]");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"KEY: '{attribute.Key,-12}' | HEX NUMERIC VALUE: {attribute.Value}");
            Console.ResetColor();
        }
    }
}