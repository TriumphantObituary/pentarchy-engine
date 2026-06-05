using System;
using Xunit;
using Pentarchy.Engine.Core.Memory;

namespace Pentarchy.Engine.Core.Tests.Memory;

/// <summary>
/// Execution validation suite for the MemoryPage infrastructure element.
/// Verifies 64-byte header coordinate tracking, fluid bitmask calculations, and slot isolation zones.
/// </summary>
public class MemoryPageTests
{
    [Fact]
    public void Constructor_Should_InitializeWithPristineCacheAlignedMetadata()
    {
        // Arrange
        ulong expectedTrackerId = 42;
        ulong expectedEntityId = 18446744073709551614; // Extreme 64-bit unsigned value
        byte expectedMode = 2; // Graph Edge Mode

        // Act
        MemoryPage page = new MemoryPage(expectedTrackerId, expectedEntityId, expectedMode);

        // Assert - Verify properties expose logical values accurately
        Assert.Equal(expectedTrackerId, page.PageTrackerId);
        Assert.Equal(expectedEntityId, page.EntityId);
        Assert.Equal(expectedMode, page.PageTypeMode);
        Assert.Equal(0U, page.AllocationMask); // Must initialize perfectly vacant
    }

    [Fact]
    public void MetadataProperties_Should_MapToExactBinaryOffsets()
    {
        // Arrange
        MemoryPage page = new MemoryPage(0, 0, 0);

        // Act - Force write explicit test values directly into properties
        page.PageTrackerId = 0x1122334455667788;
        page.EntityId      = 0xAABBCCDDEEFF0011;
        page.PageTypeMode  = 0x7F;

        // Assert - Inspect the raw backing byte array buffer to prove strict layout alignment
        Span<byte> rawBytes = page.Buffer.Span;

        // Bytes 0-7: PageTrackerId (Little Endian Verification)
        Assert.Equal(0x88, rawBytes[0]);
        Assert.Equal(0x11, rawBytes[7]);

        // Bytes 8-15: EntityId (Little Endian Verification)
        Assert.Equal(0x11, rawBytes[8]);
        Assert.Equal(0xAA, rawBytes[15]);

        // Byte 16: Structural Typology Mode
        Assert.Equal(0x7F, rawBytes[16]);
    }

    [Fact]
    public void AllocateAndFree_Should_ToggleAllocationMaskBitsNatively()
    {
        // Arrange
        MemoryPage page = new MemoryPage(0, 0, 0);
        Span<byte> dummyPrimitive = stackalloc byte[MemoryPage.SlotSize];
        dummyPrimitive.Fill(0xAA);

        // Verify state prior to operation
        Assert.False(page.IsSlotActive(0));
        Assert.False(page.IsSlotActive(5));

        // Act - Allocate Slot 0 and Slot 5
        page.AllocateSlot(0, dummyPrimitive);
        page.AllocateSlot(5, dummyPrimitive);

        // Assert - Verify bitmask bits are flipped to active (1U << 0 | 1U << 5 => 1 + 32 = 33)
        Assert.True(page.IsSlotActive(0));
        Assert.True(page.IsSlotActive(5));
        Assert.False(page.IsSlotActive(1));
        Assert.Equal(33U, page.AllocationMask);

        // Act - Free Slot 0
        page.FreeSlot(0);

        // Assert - Verify bitmask bit is cleanly dropped back to 0 (33 - 1 = 32)
        Assert.False(page.IsSlotActive(0));
        Assert.True(page.IsSlotActive(5));
        Assert.Equal(32U, page.AllocationMask);
    }

    [Fact]
    public void AllocateSlot_Should_IsolatePayloadToExactMemorySlotCoordinates()
    {
        // Arrange
        MemoryPage page = new MemoryPage(0, 0, 0);
        Span<byte> sourcePayload = stackalloc byte[MemoryPage.SlotSize];
        sourcePayload.Fill(0xBB);

        int targetSlot = 2;
        // Calculation: Header (64) + SlotIndex (2) * SlotSize (32) = Byte 128
        int expectedPhysicalOffset = 128; 

        // Act
        page.AllocateSlot(targetSlot, sourcePayload);

        // Assert - Read directly out of the raw buffer at the expected coordinate tracks
        ReadOnlySpan<byte> rawSlotSlice = page.Buffer.Span.Slice(expectedPhysicalOffset, MemoryPage.SlotSize);
        foreach (byte b in rawSlotSlice)
        {
            Assert.Equal(0xBB, b);
        }

        // Verify that the preceding byte layout region remains untouched clean padding (0x00)
        Assert.Equal(0x00, page.Buffer.Span[expectedPhysicalOffset - 1]);
    }

    [Theory]
    [InlineData(0, 4)]    // Deep inside PageTrackerId zone
    [InlineData(15, 1)]   // Edge boundary of EntityId zone
    [InlineData(16, 1)]   // PageTypeMode position
    [InlineData(17, 4)]   // AllocationMask register lane
    [InlineData(63, 1)]   // Terminal padding byte edge of header
    public void DirectWriteAndRead_Should_ThrowException_When_ViolatingHeaderGuardrails(int illegalOffset, int length)
    {
        // Arrange
        MemoryPage page = new MemoryPage(0, 0, 0);
        byte[] dummyData = new byte[length];

        // Act & Assert - Direct writes to infrastructure tracking zone must be rejected
        Assert.Throws<ArgumentOutOfRangeException>(() => page.Write(illegalOffset, dummyData));

        // Act & Assert - Direct reads bypassing property channels must be rejected
        Assert.Throws<ArgumentOutOfRangeException>(() => page.Read(illegalOffset, length));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(30)] // Out of bounds index (0 to 29 are our 30 valid sandbox slots)
    public void SlotOperations_Should_ThrowException_When_SlotIndexIsOutOfBounds(int invalidSlotIndex)
    {
        // Arrange
        MemoryPage page = new MemoryPage(0, 0, 0);
        byte[] dummyData = new byte[MemoryPage.SlotSize];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AllocateSlot(invalidSlotIndex, dummyData));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.FreeSlot(invalidSlotIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.IsSlotActive(invalidSlotIndex));
    }
}