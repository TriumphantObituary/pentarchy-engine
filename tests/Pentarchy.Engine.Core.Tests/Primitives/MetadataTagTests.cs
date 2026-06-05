using System;
using Xunit;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Primitives;

/// <summary>
/// Execution validation suite for the MetadataTag qualitative primitive.
/// Enforces character-to-byte boundaries, zero-allocation serialization safety, and memory-padding sanitation.
/// </summary>
public class MetadataTagTests
{
    [Fact]
    public void Constructor_Should_AcceptValidStrings()
    {
        // Arrange
        string expectedTag = "Type:Enemy_Unit";

        // Act
        MetadataTag tag = new MetadataTag(expectedTag);

        // Assert
        Assert.Equal(expectedTag, tag.Value);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_ValueIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MetadataTag(null!));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentException_When_TagExceeds32Bytes()
    {
        // Arrange - An ASCII string that is exactly 39 bytes long, violating our 32-byte slot ceiling
        string massiveTag = "System:Module:Subsystem:LongTagOverflow";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MetadataTag(massiveTag));
    }

    [Fact]
    public void Constructor_Should_EnforceByteLimitsOnMultiByteUtf8Symbols()
    {
        // Arrange
        // Each of these skull symbols '💀' takes exactly 4 bytes in UTF-8.
        // 8 characters * 4 bytes = 32 bytes (Perfect fit on the boundary)
        string validSymbolTag = "💀💀💀💀💀💀💀💀";
        
        // 9 characters * 4 bytes = 36 bytes (Violates our 32-byte slot ceiling)
        string invalidSymbolTag = "💀💀💀💀💀💀💀💀💀";

        // Act & Assert - Valid byte footprint must pass cleanly
        var tag = new MetadataTag(validSymbolTag);
        Assert.Equal(validSymbolTag, tag.Value);

        // Act & Assert - Invalid byte footprint must be caught immediately
        Assert.Throws<ArgumentException>(() => new MetadataTag(invalidSymbolTag));
    }

    [Fact]
    public void Serialization_RoundTrip_Should_MaintainAbsoluteDataIntegrityBypassingHeap()
    {
        // Arrange
        MetadataTag originalTag = new MetadataTag("Faction:Vanguard_Elite");
        Span<byte> preAllocatedBuffer = stackalloc byte[MetadataTag.MaxTagSize];

        // Act
        originalTag.Serialize(preAllocatedBuffer);
        MetadataTag rehydratedTag = MetadataTag.Deserialize(preAllocatedBuffer);

        // Assert
        Assert.Equal(originalTag.Value, rehydratedTag.Value);
        Assert.Equal(originalTag, rehydratedTag);
    }

    [Fact]
    public void Serialize_Should_ThrowArgumentException_When_BufferIsTooSmall()
    {
        // Arrange
        MetadataTag tag = new MetadataTag("Test:Tag");
        byte[] shortBuffer = new byte[31]; // 1 byte short of the structural 32B wall

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tag.Serialize(shortBuffer));
    }

    [Fact]
    public void Deserialize_Should_ThrowArgumentException_When_BufferLengthIsIncorrect()
    {
        // Arrange
        byte[] overflowBuffer = new byte[33]; // Violates physical slot layout alignment

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MetadataTag.Deserialize(overflowBuffer));
    }

    [Fact]
    public void Serialize_Should_CompletelyClearDestinationBufferPadding()
    {
        // Arrange
        MetadataTag tag = new MetadataTag("ShortTag"); // Length 8
        Span<byte> buffer = stackalloc byte[MetadataTag.MaxTagSize];
        
        // Pollute the entire buffer target with dirty mock bytes (0xFF)
        buffer.Fill(0xFF);

        // Act
        tag.Serialize(buffer);

        // Assert - Verify trailing space from index 8 up to 31 has been sanitized to clean null 0x00 padding
        for (int i = 8; i < MetadataTag.MaxTagSize; i++)
        {
            Assert.Equal(0x00, buffer[i]);
        }
    }

    [Fact]
    public void EqualityOverloads_Should_VerifyByValueBypassingBoxMechanics()
    {
        // Arrange
        MetadataTag tagA = new MetadataTag("State:Active");
        MetadataTag tagB = new MetadataTag("State:Active");
        MetadataTag tagC = new MetadataTag("State:Dead");

        // Act & Assert
        Assert.True(tagA == tagB);
        Assert.False(tagA == tagC);
        Assert.True(tagA != tagC);
        Assert.True(tagA.Equals(tagB));
        Assert.False(tagA.Equals(tagC));
        Assert.Equal(tagA.GetHashCode(), tagB.GetHashCode());
    }
}