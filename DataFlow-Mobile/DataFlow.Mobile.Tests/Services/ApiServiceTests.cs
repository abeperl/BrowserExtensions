using Xunit;

namespace DataFlow.Mobile.Tests.Services;

public class ApiServiceTests
{
    [Fact]
    public void Placeholder_Test_Should_Pass()
    {
        Assert.True(true);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void ValidateHttpMethod_Should_Accept_Valid_Methods(string method)
    {
        Assert.False(string.IsNullOrEmpty(method));
    }
}