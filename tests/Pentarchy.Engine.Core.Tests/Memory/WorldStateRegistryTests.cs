using System;
using Xunit;
using Pentarchy.Engine.Core.Memory;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Memory;

/// <summary>
/// Execution validation suite for the WorldStateRegistry matrix layer.
/// Enforces multi-page configuration indexing, typology-safe pipeline isolating, and safety interceptors.
/// </summary>
public class WorldStateRegistryTests
{
    [Fact]
    public void Constructor_Should_AllocateRequestedPageVolumeAndSetDefaults()
    {
        // Arrange & Act
        int expectedPages = 4;
        WorldStateRegistry registry = new WorldStateRegistry(expectedPages);

        // Assert
        Assert.NotNull(registry);
        // Pages must initialize as completely anonymous systems (Owner 0, Mode 0)
        Assert.Equal(0UL, registry.GetEntityOwnerOfPage(3));
    }

    [Fact]
    public void Registry_Should_ManagePageOwnershipAndTypologyLifecycle()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(2);
        ulong expectedEntity = 10042;
        byte expectedMode = 1; // Metadata Tag Mode

        // Act
        registry.AssignPageToEntity(pageIndex: 1, expectedEntity, expectedMode);

        // Assert
        Assert.Equal(expectedEntity, registry.GetEntityOwnerOfPage(1));
        Assert.Equal(0UL, registry.GetEntityOwnerOfPage(0)); // Page 0 remains unassigned
    }

    [Fact]
    public void CommitAndFetch_Should_MaintainDataIntegrityAcrossPageBoundaries()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 3);
        // Ensure Page 2 is explicitly configured for Attribute Mode (0)
        registry.AssignPageToEntity(pageIndex: 2, entityId: 500, pageTypeMode: 0);

        string key = "Target_Stat";
        double expectedValue = 999.34;
        AttributeTuple inputAttr = new AttributeTuple(key, expectedValue);

        // Act - Commit deep into Page 2, Slot 5 (verifying offset calculation logic)
        registry.CommitAttribute(pageIndex: 2, slotIndex: 5, inputAttr);
        AttributeTuple retrievedAttr = registry.FetchAttribute(pageIndex: 2, slotIndex: 5);

        // Assert
        Assert.Equal(key, retrievedAttr.Key);
        Assert.Equal(expectedValue, retrievedAttr.Value);
    }

    [Fact]
    public void Registry_Should_EnforceStrictPageTypologyIsolation()
    {
        // Arrange - Page 0 is Attribute Mode (0), Page 1 is Metadata Tag Mode (1)
        WorldStateRegistry registry = new WorldStateRegistry(2);
        registry.AssignPageToEntity(pageIndex: 0, entityId: 777, pageTypeMode: 0);
        registry.AssignPageToEntity(pageIndex: 1, entityId: 777, pageTypeMode: 1);

        AttributeTuple attribute = new AttributeTuple("Strength", 15.0);
        MetadataTag tag = new MetadataTag("Status:Poisoned");

        // Act & Assert - Attempting to write a tag to an attribute page must reject
        Assert.Throws<InvalidOperationException>(() => registry.CommitTag(pageIndex: 0, slotIndex: 0, tag));

        // Act & Assert - Attempting to write an attribute to a tag page must reject
        Assert.Throws<InvalidOperationException>(() => registry.CommitAttribute(pageIndex: 1, slotIndex: 0, attribute));
    }

    [Fact]
    public void Fetch_Should_ThrowInvalidOperationException_When_SlotIsUnallocated()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(1);
        registry.AssignPageToEntity(pageIndex: 0, entityId: 100, pageTypeMode: 0);

        // Act & Assert - Fetching an untouched slot must trigger the allocation bitmask guard
        var exception = Assert.Throws<InvalidOperationException>(() => 
        {
            registry.FetchAttribute(pageIndex: 0, slotIndex: 15);
        });

        Assert.Contains("Read Violation", exception.Message);
    }

    [Theory]
    [InlineData(-1, 0)] // Negative page bounds violation
    [InlineData(3, 0)]  // Out-of-bounds page index (Array size is 3, valid handles: 0, 1, 2)
    [InlineData(0, -1)] // Negative slot index violation
    [InlineData(0, 30)] // Out-of-bounds slot index (1KB sandbox max slots is 30, indices: 0-29)
    public void BoundaryChecks_Should_ThrowArgumentOutOfRangeException_OnLayoutViolation(int pageIndex, int slotIndex)
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 3);
        AttributeTuple attr = new AttributeTuple("Fault_Key", 1.0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => registry.CommitAttribute(pageIndex, slotIndex, attr));
    }

    [Fact]
    public void CommitAttribute_Should_CompletelyOverwriteExistingSlotData()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(pageCount: 1);
        registry.AssignPageToEntity(pageIndex: 0, entityId: 1, pageTypeMode: 0);

        AttributeTuple initialAttr = new AttributeTuple("VeryLongInitialKey", 111.11);
        AttributeTuple overwritingAttr = new AttributeTuple("TinyKey", 222.22);

        // Act
        registry.CommitAttribute(pageIndex: 0, slotIndex: 2, initialAttr);
        // Overwrite the precise coordinate slot with shorter key metrics
        registry.CommitAttribute(pageIndex: 0, slotIndex: 2, overwritingAttr);
        
        AttributeTuple result = registry.FetchAttribute(pageIndex: 0, slotIndex: 2);

        // Assert - Verify trailing buffer residue from long key didn't pollute rehydration
        Assert.Equal("TinyKey", result.Key);
        Assert.Equal(222.22, result.Value);
    }

    [Fact]
    public void Registry_Should_SuccessfullyRoundTrip_AllPrimitiveVarieties()
    {
        // Arrange
        WorldStateRegistry registry = new WorldStateRegistry(3);
        registry.AssignPageToEntity(pageIndex: 0, entityId: 888, pageTypeMode: 0); // Attribute
        registry.AssignPageToEntity(pageIndex: 1, entityId: 888, pageTypeMode: 1); // Tag
        registry.AssignPageToEntity(pageIndex: 2, entityId: 888, pageTypeMode: 2); // Graph Edge

        AttributeTuple expectedAttribute = new AttributeTuple("Mana", 50.0);
        MetadataTag    expectedTag       = new MetadataTag("Zone:Caelum");
        GraphEdge      expectedEdge      = new GraphEdge(55, 66, 1.5, 8);

        // Act
        registry.CommitAttribute(pageIndex: 0, slotIndex: 0, expectedAttribute);
        registry.CommitTag(pageIndex: 1, slotIndex: 14, expectedTag);
        registry.CommitEdge(pageIndex: 2, slotIndex: 29, expectedEdge);

        // Assert
        Assert.Equal(expectedAttribute, registry.FetchAttribute(pageIndex: 0, slotIndex: 0));
        Assert.Equal(expectedTag,       registry.FetchTag(pageIndex: 1, slotIndex: 14));
        Assert.Equal(expectedEdge,      registry.FetchEdge(pageIndex: 2, slotIndex: 29));
    }
}