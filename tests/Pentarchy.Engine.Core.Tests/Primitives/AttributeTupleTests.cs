using System;
using Xunit;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Core.Tests.Primitives;

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
    public void Constructor_Should_ThrowArgumentException_When_KeyExceedsSerialisationLimit()
    {
        // Arrange
        // This key is exactly 25 characters long, violating our 24-byte cache allocation ceiling
        string illegalLongKey = "InvalidLengthKeyString25C"; 
        double dummyValue = 5.0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new AttributeTuple(illegalLongKey, dummyValue);
        });
    }
}