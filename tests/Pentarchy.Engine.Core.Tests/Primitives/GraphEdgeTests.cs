using System;
using Xunit;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Primitives;

/// <summary>
/// Execution validation suite for the GraphEdge relational primitive.
/// Enforces layout-boundary compliance, zero-allocation serialization, and flag-bitfield purity.
/// </summary>
public class GraphEdgeTests
{
    [Fact]
    public void Constructor_Should_CorrectlyAssignProperties()
    {
        // Arrange
        ulong expectedSource = 500;
        ulong expectedDest = 600;
        double expectedCost = 12.34;
        uint expectedFlags = 7;

        // Act
        GraphEdge edge = new GraphEdge(expectedSource, expectedDest, expectedCost, expectedFlags);

        // Assert
        Assert.Equal(expectedSource, edge.SourceNodeId);
        Assert.Equal(expectedDest, edge.DestinationNodeId);
        Assert.Equal(expectedCost, edge.TraversalCost);
        Assert.Equal(expectedFlags, edge.Flags);
    }

    [Fact]
    public void Serialization_RoundTrip_Should_MaintainPerfectPrecisionBypassingHeap()
    {
        // Arrange
        ulong expectedSource = 1001;
        ulong expectedDest = 2002;
        double expectedCost = 54.321;
        uint expectedFlags = 0b0000_0000_0000_0000_0000_0000_0000_0011; // 3 (Locked | Traversed)

        GraphEdge originalEdge = new GraphEdge(expectedSource, expectedDest, expectedCost, expectedFlags);
        Span<byte> preAllocatedBuffer = stackalloc byte[GraphEdge.SizeInBytes];

        // Act
        originalEdge.Serialize(preAllocatedBuffer);
        GraphEdge rehydratedEdge = GraphEdge.Deserialize(preAllocatedBuffer);

        // Assert
        Assert.Equal(expectedSource, rehydratedEdge.SourceNodeId);
        Assert.Equal(expectedDest, rehydratedEdge.DestinationNodeId);
        Assert.Equal(expectedCost, rehydratedEdge.TraversalCost);
        Assert.Equal(expectedFlags, rehydratedEdge.Flags);
        Assert.Equal(originalEdge, rehydratedEdge);
    }

    [Fact]
    public void Serialize_Should_ThrowArgumentException_When_BufferIsTooSmall()
    {
        // Arrange
        GraphEdge edge = new GraphEdge(1, 2, 1.0, 0);
        byte[] invalidSmallBuffer = new byte[31]; // 1 byte short of the structural 32B wall

        // Act & Assert
        Assert.Throws<ArgumentException>(() => edge.Serialize(invalidSmallBuffer));
    }

    [Fact]
    public void Deserialize_Should_ThrowArgumentException_When_BufferLengthIsIncorrect()
    {
        // Arrange
        byte[] invalidBuffer = new byte[33]; // Overflow boundary

        // Act & Assert
        Assert.Throws<ArgumentException>(() => GraphEdge.Deserialize(invalidBuffer));
    }

    [Fact]
    public void Serialize_Should_CleanlySanitizeTrailingPadding()
    {
        // Arrange
        GraphEdge edge = new GraphEdge(10, 20, 5.5, 42);
        Span<byte> buffer = stackalloc byte[GraphEdge.SizeInBytes];
        
        // Intentionally pollute the destination padding area (Bytes 28-31) with garbage values
        buffer.Slice(28, 4).Fill(0xFF);

        // Act
        edge.Serialize(buffer);

        // Assert - Verify that padding bytes 28-31 were explicitly zeroed out during serialization
        for (int i = 28; i < 32; i++)
        {
            Assert.Equal(0x00, buffer[i]);
        }
    }

    [Fact]
    public void BitmaskFlags_Should_CombineAndEvaluateCorrectly()
    {
        // Arrange - Define multi-flag bits using standard shift mechanics
        uint flagIsWaterPath = 1 << 0;  // 1
        uint flagIsDangerous = 1 << 1;  // 2
        uint flagRequiresKey = 1 << 2;  // 4

        uint activeFlags = flagIsWaterPath | flagRequiresKey;

        // Act
        GraphEdge edge = new GraphEdge(1, 2, 10.0, activeFlags);

        // Assert - Verify status switches using bitwise AND operations
        Assert.True((edge.Flags & flagIsWaterPath) != 0, "Water path flag should be active.");
        Assert.True((edge.Flags & flagRequiresKey) != 0, "Requires key flag should be active.");
        Assert.False((edge.Flags & flagIsDangerous) != 0, "Dangerous path flag should remain inactive.");
    }

    [Fact]
    public void EqualityOverloads_Should_VerifyByValueWithoutBoxing()
    {
        // Arrange
        GraphEdge edgeA = new GraphEdge(10, 20, 1.5, 0);
        GraphEdge edgeB = new GraphEdge(10, 20, 1.5, 0);
        GraphEdge edgeC = new GraphEdge(10, 99, 1.5, 0);

        // Act & Assert
        Assert.True(edgeA == edgeB);
        Assert.False(edgeA == edgeC);
        Assert.True(edgeA != edgeC);
        Assert.True(edgeA.Equals(edgeB));
        Assert.False(edgeA.Equals(edgeC));
        Assert.Equal(edgeA.GetHashCode(), edgeB.GetHashCode());
    }
}