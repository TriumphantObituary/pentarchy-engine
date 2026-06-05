using System;
using Xunit;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Primitives;

/// <summary>
/// Execution validation suite for the AttributeTuple primitive.
/// Enforces structural constraint invariants, boundary sizes, and unboxed serialization stability.
/// </summary>
public class AttributeTupleTests
{
    [Fact]
    public void Constructor_Should_CorrectlyAssignProperties()
    {
        // Arrange
        string expectedKey = "Health_Pool";
        double expectedValue = 100.0;

        // Act
        AttributeTuple tuple = new AttributeTuple(expectedKey, expectedValue);

        // Assert
        Assert.Equal(expectedKey, tuple.Key);
        Assert.Equal(expectedValue, tuple.Value);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_KeyIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AttributeTuple(null!, 5.0));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentException_When_KeyExceedsByteCeiling()
    {
        // Arrange - This ASCII key is 25 characters/bytes long, violating the 24-byte tracking track
        string illegalLongKey = "InvalidLengthKeyString25C"; 
        double dummyValue = 5.0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AttributeTuple(illegalLongKey, dummyValue));
    }

    [Fact]
    public void Constructor_Should_AccountForMultiByteUtf8Symbols()
    {
        // Arrange
        // Each of these fire emojis '🔥' takes up exactly 4 bytes in UTF-8.
        // 6 characters * 4 bytes = 24 bytes (Perfect fit on the boundary limit)
        string validMultiByteKey = "🔥🔥🔥🔥🔥🔥";
        
        // 7 characters * 4 bytes = 28 bytes (Explodes our 24-byte structural wall)
        string invalidMultiByteKey = "🔥🔥🔥🔥🔥🔥🔥";

        // Act & Assert - Valid byte footprint must pass despite low character count
        var validTuple = new AttributeTuple(validMultiByteKey, 10.0);
        Assert.Equal(validMultiByteKey, validTuple.Key);

        // Act & Assert - Invalid byte footprint must be intercepted natively
        Assert.Throws<ArgumentException>(() => new AttributeTuple(invalidMultiByteKey, 10.0));
    }

    [Fact]
    public void Serialization_RoundTrip_Should_PreservePerfectBitPrecision()
    {
        // Arrange
        AttributeTuple original = new AttributeTuple("Speed_Modifier", -45.6789);
        Span<byte> preAllocatedBuffer = stackalloc byte[AttributeTuple.SizeInBytes];

        // Act
        original.Serialize(preAllocatedBuffer);
        AttributeTuple rehydrated = AttributeTuple.Deserialize(preAllocatedBuffer);

        // Assert
        Assert.Equal(original.Key, rehydrated.Key);
        Assert.Equal(original.Value, rehydrated.Value);
        Assert.Equal(original, rehydrated);
    }

    [Fact]
    public void EqualityOverloads_Should_VerifyByValueBypassingBoxMechanics()
    {
        // Arrange
        AttributeTuple instanceA = new AttributeTuple("Atk_Power", 55.0);
        AttributeTuple instanceB = new AttributeTuple("Atk_Power", 55.0);
        AttributeTuple instanceC = new AttributeTuple("Def_Power", 55.0);

        // Act & Assert
        Assert.True(instanceA == instanceB);
        Assert.False(instanceA == instanceC);
        Assert.True(instanceA != instanceC);
        Assert.True(instanceA.Equals(instanceB));
        Assert.False(instanceA.Equals(instanceC));
        Assert.Equal(instanceA.GetHashCode(), instanceB.GetHashCode());
    }

    [Fact]
    public void Deserialization_Should_ThrowArgumentException_When_BufferLengthIsIncorrect()
    {
        // Arrange - Create a dirty, non-conforming memory segment size (31 bytes instead of 32)
        byte[] invalidBuffer = new byte[31];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AttributeTuple.Deserialize(invalidBuffer));
    }
}