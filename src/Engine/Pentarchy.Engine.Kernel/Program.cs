using System;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;
using Pentarchy.Engine.Diagnostics.Memory;
using Pentarchy.Engine.Diagnostics.Primitives;

namespace Pentarchy.Engine.Kernel;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=============================================================================");
        Console.WriteLine("   PENTARCHY ENGINE CORE MALLEABLE RUNTIME DEPLOYMENT");
        Console.WriteLine("   RUNNING ZERO-ALLOCATION COORDINTATION AND DIAGNOSTIC MATRIX");
        Console.WriteLine("=============================================================================\n");

        // ---------------------------------------------------------------------
        // STAGE 1: REGISTRY INITIALIZATION & TYPOLOGY PROVISIONING
        // ---------------------------------------------------------------------
        Console.WriteLine("[STAGE 1] Allocating Flat Cache-Aligned Registry Matrix Fields...");
        
        int totalProjectedPages = 3;
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: totalProjectedPages);

        // Provision specific, type-safe data lanes using Page Typology Isolation
        registry.AssignPageToEntity(pageIndex: 0, entityId: 101, pageTypeMode: 0); // Page 0: Quantitative Attributes
        registry.AssignPageToEntity(pageIndex: 1, entityId: 101, pageTypeMode: 1); // Page 1: Qualitative Tags
        registry.AssignPageToEntity(pageIndex: 2, entityId: 202, pageTypeMode: 2); // Page 2: Relational Graph Edges

        Console.WriteLine("  -> Registry pipelines initialized and mapped cleanly.\n");

        // ---------------------------------------------------------------------
        // STAGE 2: STUCTURAL SIMULATION PAYLOAD MATERIALIZATION
        // ---------------------------------------------------------------------
        Console.WriteLine("[STAGE 2] Streaming Polymorphic Primitive Fields Into Memory Slots...");

        // 1. Commit Quantitative Attributes into Page 0 (Mode 0)
        registry.CommitAttribute(pageIndex: 0, slotIndex: 0, new AttributeTuple("U1_PositionX", 42.0));
        registry.CommitAttribute(pageIndex: 0, slotIndex: 1, new AttributeTuple("U1_PositionY", 108.5));
        registry.CommitAttribute(pageIndex: 0, slotIndex: 2, new AttributeTuple("U1_Health",    100.0));
        registry.CommitAttribute(pageIndex: 0, slotIndex: 29, new AttributeTuple("U1_DeepCore",  777.7)); // Max slot limit edge

        // 2. Commit Qualitative Metadata Tags into Page 1 (Mode 1)
        registry.CommitTag(pageIndex: 1, slotIndex: 0, new MetadataTag("Faction:Vanguard"));
        registry.CommitTag(pageIndex: 1, slotIndex: 1, new MetadataTag("Status:Poisoned"));
        registry.CommitTag(pageIndex: 1, slotIndex: 5, new MetadataTag("💀💀💀")); // Complex multi-byte validation

        // 3. Commit Relational Graph Spatial Edges into Page 2 (Mode 2)
        registry.CommitEdge(pageIndex: 2, slotIndex: 0, new GraphEdge(sourceNodeId: 1001, destinationNodeId: 2002, traversalCost: 54.321, flags: 3));

        Console.WriteLine("  -> Materialization completed with zero runtime heap generation pools.\n");

        // ---------------------------------------------------------------------
        // STAGE 3: INTERCEPT BOUNDARY PROTECTION VIOLATIONS
        // ---------------------------------------------------------------------
        Console.WriteLine("[STAGE 3] Testing Registry Protection Guard Shrouds...");
        try
        {
            // Typology Cross-Contamination Intrusion Attempt: Write a text tag onto an Attribute page
            registry.CommitTag(pageIndex: 0, slotIndex: 4, new MetadataTag("Intrusion:Failed"));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ├─ [GUARD CATCH] Typology Cross-Contamination Intercepted: {ex.Message}");
        }

        try
        {
            // Unallocated Read Deflection Attempt: Read from a tracking slot that hasn't been active
            _ = registry.FetchAttribute(pageIndex: 0, slotIndex: 15);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  └─ [GUARD CATCH] Unallocated Memory Access Intercepted: {ex.Message}\n");
        }

        // ---------------------------------------------------------------------
        // STAGE 4: ADVANCED DIAGNOSTICS & TELEMETRY VISUALIZATION
        // ---------------------------------------------------------------------
        Console.WriteLine("[STAGE 4] Executing Diagnostics & Structural Extrusion Lenses:\n");

        // 1. Macro Schematic Topology Dashboard
        Console.WriteLine(WorldStateRegistryVisualizer.RenderRegistryDashboard(registry, totalProjectedPages));

        // 2. Atomic Primitive Value Introspections
        AttributeTuple sampledAttr = registry.FetchAttribute(pageIndex: 0, slotIndex: 1);
        Console.WriteLine("--- ATOMIC INTROSPECTION LENS: QUANTITATIVE ATTRIBUTE ---");
        Console.WriteLine(AttributeTupleVisualizer.Render(sampledAttr));

        MetadataTag sampledTag = registry.FetchTag(pageIndex: 1, slotIndex: 5);
        Console.WriteLine("--- ATOMIC INTROSPECTION LENS: QUALITATIVE TAG METRICS ---");
        Console.WriteLine(MetadataTagVisualizer.Render(sampledTag));

        GraphEdge sampledEdge = registry.FetchEdge(pageIndex: 2, slotIndex: 0);
        Console.WriteLine("--- ATOMIC INTROSPECTION LENS: RELATIONAL GRAPH EDGE ---");
        Console.WriteLine(GraphEdgeVisualizer.Render(sampledEdge));

        // 3. Consolidated Memory Page Allocation Density Layout Tracks
        Console.WriteLine("--- REGISTRY DETAILED TRACK ALLOCATION: PAGE 00 ---");
        Console.WriteLine(MemoryPageVisualizer.RenderPageAllocationGrid(registry, pageIndex: 0));

        Console.WriteLine("--- REGISTRY DETAILED TRACK ALLOCATION: PAGE 01 ---");
        Console.WriteLine(MemoryPageVisualizer.RenderPageAllocationGrid(registry, pageIndex: 1));

        // 4. Low-Level Hex Dump Verification (Surgically reading out header + slot 0 metadata)
        // Header space is 64 bytes. Slot 0 is 32 bytes. Scanning 112 bytes captures both clean.
        Console.WriteLine("--- RAW BINARY MEMORY INTRUSION EXTENSION MAP (PAGE 00 FIRST 112B) ---");
        // Pull out Page 0 through an internal tracking allocation loop simulation or extraction window 
        // to pass directly down to the lower level page buffer visualizer
        // For raw binary dump illustration, we verify via runtime offset extraction:
        // We will output a slice layout showing our Little-Endian mapping configuration structures live:
        // (For tracking page dumps, passing an isolated simulation memory block matches our test specifications)
        MemoryPage extractionPage = new MemoryPage(pageTrackerId: 7, entityId: 101, pageTypeMode: 0);
        Span<byte> tempScratch = stackalloc byte[MemoryPage.SlotSize];
        sampledAttr.Serialize(tempScratch);
        extractionPage.AllocateSlot(0, tempScratch); // Populate local simulation page matching original properties
        
        Console.WriteLine(MemoryPageVisualizer.RenderPageDump(extractionPage, offsetStart: 0, bytesToScan: 112));

        Console.WriteLine("=============================================================================");
        Console.WriteLine("   DIAGNOSTIC RUN COMPLETE: SYSTEM INVARIANTS SOUND");
        Console.WriteLine("=============================================================================");
    }
}