using System;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Kernel;

class Program
{
    static void Main(string[] args)
    {
        LogHeader();

        // 1. Initialize our Mock Packet Universe
        Guid testPacketId = Guid.NewGuid();
        EpochToken initialEpoch = new EpochToken(Guid.NewGuid(), 1);
        
        // We set the initial TTL low (3) so we can watch it expire quickly in our logs
        PacketHeader packet = new PacketHeader(
            PacketId: testPacketId,
            Ttl: 3,
            LatencyBudget: TimeSpan.FromMilliseconds(2),
            Qos: QosClass.CriticalRealTime,
            Epoch: initialEpoch
        );

        LogInfo("SYSTEM", $"Kernel Bootstrapped. Primordial packet generated with TTL: {packet.Ttl}");

        // 2. Simulate the Packet travelling across the Virtual Bus Loop
        int currentTick = 1;
        while (currentTick <= 5)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n--- [FAST BUS TICK #{currentTick:00}] ---");
            Console.ResetColor();

            // Guard Check: Verify the Packet hasn't run out of energy (TTL)
            if (packet.Ttl <= 0)
            {
                LogError("DROP", $"Packet {packet.PacketId} aborted execution context.");
                LogError("WARN", $"Reason: Time-To-Live hit 0. Infinite modification loop halted.");
                break; 
            }

            // Simulate an interception module handling the packet
            LogRouting($"Module 'Game.Combat.MockSolver' in Slot 3 intercepted packet.");
            
            // The module "processes" it, which immutably decrements the TTL
            packet = packet.DecrementTtl();
            LogInfo("ROUTING", $"Packet forwarded down the line. Remaining TTL: {packet.Ttl}");

            currentTick++;
        }

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\n=============================================");
        LogInfo("SYSTEM", "Engine loop cycle verification test complete.");
        Console.WriteLine("=============================================\n");
    }

    #region Telemetry Logger Infrastructure
    
    static void LogHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("=========================================================");
        Console.WriteLine("    PENTARCHY PLATFORM ENGINE - LEVEL 3 KERNEL HOST       ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();
    }

    static void LogInfo(string context, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"[{context}]");
        Console.ResetColor();
        Console.WriteLine($" [{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    static void LogRouting(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("[ROUTING]");
        Console.ResetColor();
        Console.WriteLine($" [{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    static void LogError(string tag, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"[{tag}]");
        Console.ResetColor();
        Console.WriteLine($" [{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    #endregion
}