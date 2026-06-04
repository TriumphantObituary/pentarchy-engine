using System;
using Xunit;
using Pentarchy.Engine.Core.Memory;

namespace Pentarchy.Engine.Core.Tests.Memory;

public class MemoryPageTests
{
    [Fact]
    public void Constructor_Should_InitializeWithProtectedHeader()
    {
        // Arrange
        Guid expectedGuid = Guid.NewGuid();

        // Act
        MemoryPage page = new MemoryPage(expectedGuid);

        // Assert
        // Rehydrate the first 16 bytes of the raw buffer to verify the GUID landed safely
        byte[] rawHeaderBytes = new byte[16];
        page.Buffer.Slice(0, 16).CopyTo(rawHeaderBytes);
        Guid actualGuid = new Guid(rawHeaderBytes);

        Assert.Equal(expectedGuid, actualGuid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(15)]
    public void Write_Should_ThrowArgumentOutOfRangeException_When_TargetingHeaderZone(int illegalOffset)
    {
        // Arrange
        MemoryPage page = new MemoryPage(Guid.NewGuid());
        byte[] dummyData = new byte[4] { 0xAA, 0xBB, 0xCC, 0xDD };

        // Act & Assert
        // Our engine rules state that writing to bytes 0-15 must throw an exception before data corruption happens
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            page.Write(illegalOffset, dummyData);
        });
    }
}