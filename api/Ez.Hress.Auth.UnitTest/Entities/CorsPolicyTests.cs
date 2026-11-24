using Ez.Hress.Auth.Entities;

namespace Ez.Hress.Auth.UnitTest.Entities;

public class CorsPolicyTests
{
    private readonly CorsPolicy _policy = CorsPolicy.FromSetting("https://www.hress.org, https://hress.org/,http://localhost:3000");

    [Theory]
    [InlineData("https://www.hress.org")]
    [InlineData("https://hress.org")]
    [InlineData("http://localhost:3000")]
    [InlineData("HTTPS://WWW.HRESS.ORG")]
    public void IsAllowed_RegisteredOrigin_ReturnsTrue(string origin)
    {
        Assert.True(_policy.IsAllowed(origin));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("https://hress.org.evil.com")]
    [InlineData("https://evilhress.org")]
    [InlineData("http://www.hress.org")]
    [InlineData("http://localhost:3001")]
    public void IsAllowed_UnknownOrLookalikeOrigin_ReturnsFalse(string? origin)
    {
        Assert.False(_policy.IsAllowed(origin));
    }

    [Fact]
    public void FromSetting_Empty_AllowsNothing()
    {
        var policy = CorsPolicy.FromSetting(null);
        Assert.Empty(policy.AllowedOrigins);
        Assert.False(policy.IsAllowed("https://www.hress.org"));
    }
}
