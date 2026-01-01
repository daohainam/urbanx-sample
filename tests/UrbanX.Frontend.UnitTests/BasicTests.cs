using Xunit;

namespace UrbanX.Frontend.UnitTests;

public class BasicTests
{
    [Fact]
    public void Test_BasicAssertion()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Equal(42, value);
    }
}
