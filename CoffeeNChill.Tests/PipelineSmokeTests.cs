using Xunit;

namespace CoffeeNChill.Tests;

public class PipelineSmokeTests
{
    [Fact]
    public void Addition_ShouldReturnExpectedResult()
    {
        int result = 2 + 3;

        Assert.Equal(5, result);
    }
}
