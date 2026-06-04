namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// Establishes the priority and scheduling lanes of the Dual-Bus network.
/// </summary>
public enum QosClass
{
    CriticalRealTime, // Bypasses queues for the Fast Bus frame clock
    StateSimulation,  // Runs standard frame loop logic
    BestEffort        // Shunted to the Slow Bus background idle cycles
}

/// <summary>
/// The Atomic Epoch Token used to guarantee transaction isolation across bus boundaries.
/// </summary>
public readonly record struct EpochToken(Guid RequestId, long GenerationCount);

/// <summary>
/// The self-governing routing envelope header for all cross-bus data streams.
/// </summary>
public record PacketHeader(
    Guid PacketId,
    int Ttl,
    TimeSpan LatencyBudget,
    QosClass Qos,
    EpochToken Epoch
)
{
    /// <summary>
    /// Immutably decrements the Time-To-Live. If TTL hits 0, the packet will be dropped by the Kernel.
    /// </summary>
    public PacketHeader DecrementTtl() => this with { Ttl = Ttl - 1 };
}