using System;
using Xunit;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Memory;

public class WorldStateRegistryTests
{
    [Fact]
    public void Constructor_Should_AllocateRequestedPageVolume()
    {
        // Arrange & Act
        int expectedPages = 4;
        WorldStateRegistry registry = new WorldStateRegistry(expectedPages);

        // Assert
        Assert.NotNull(registry);
    }

    [Fact]
    public void CommitAndFetch_Should_MaintainDataIntegrityAcrossPageBoundaries()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 3);
        string key = "Target_Stat";
        double expectedValue = 999.34;
        AttributeTuple inputAttr = new AttributeTuple(key, expectedValue);

        // Act
        // Write deep into Page 2, Slot 5 (verifying cross-page indexing logic)
        registry.CommitAttribute(pageIndex: 2, slotIndex: 5, inputAttr);
        AttributeTuple retrievedAttr = registry.FetchAttribute(pageIndex: 2, slotIndex: 5);

        // Assert
        Assert.Equal(key, retrievedAttr.Key);
        Assert.Equal(expectedValue, retrievedAttr.Value);
    }

    [Theory]
    [InlineData(-1, 0)]  // Negative page bounds
    [InlineData(3, 0)]   // Out-of-bounds page index (Size is 3, max index is 2)
    [InlineData(0, -1)]  // Negative slot bounds
    [InlineData(0, 32)]  // Out-of-bounds slot index (Max slot is 31)
    public void CommitAttribute_Should_ThrowArgumentOutOfRangeException_OnBoundaryViolation(int pageIndex, int slotIndex)
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 3);
        AttributeTuple attr = new AttributeTuple("Fault_Key", 1.0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            registry.CommitAttribute(pageIndex, slotIndex, attr);
        });
    }

    [Fact]
    public void CommitAttribute_Should_CompletelyOverwriteExistingSlotData()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 1);
        AttributeTuple initialAttr = new AttributeTuple("VeryLongInitialKey", 111.11);
        AttributeTuple overwritingAttr = new AttributeTuple("TinyKey", 222.22);

        // Act
        registry.CommitAttribute(pageIndex: 0, slotIndex: 2, initialAttr);
        // Fire the second write onto the exact same coordinates
        registry.CommitAttribute(pageIndex: 0, slotIndex: 2, overwritingAttr);
        
        AttributeTuple result = registry.FetchAttribute(pageIndex: 0, slotIndex: 2);

        // Assert
        Assert.Equal("TinyKey", result.Key);
        Assert.Equal(222.22, result.Value);
    }

    [Fact]
    public void FetchAttribute_Should_ReturnEmptyTuple_When_SlotIsUnallocated()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 1);

        // Act
        // Peek at Slot 15 which has never been touched
        AttributeTuple result = registry.FetchAttribute(pageIndex: 0, slotIndex: 15);

        // Assert
        Assert.Equal(string.Empty, result.Key);
        Assert.Equal(0.0, result.Value);
    }

    [Fact]
    public void CommitAttribute_Should_PreserveHighPrecisionFloatingPointBits()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 1);
        double highPrecisionValue = 0.123456789012345; 
        AttributeTuple complexAttr = new AttributeTuple("Precision_Test", highPrecisionValue);

        // Act
        registry.CommitAttribute(pageIndex: 0, slotIndex: 0, complexAttr);
        AttributeTuple result = registry.FetchAttribute(pageIndex: 0, slotIndex: 0);

        // Assert
        Assert.Equal(highPrecisionValue, result.Value);
    }
}