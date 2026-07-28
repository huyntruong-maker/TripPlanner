using FluentAssertions;

namespace Tests;

public class PlaceholderTests
{
    [Fact]
    public void True_Always_IsTrue()
    {
        true.Should().BeTrue();
    }
}
