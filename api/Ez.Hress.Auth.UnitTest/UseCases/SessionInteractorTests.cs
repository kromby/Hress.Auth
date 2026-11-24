using Ez.Hress.Auth.Entities;
using Ez.Hress.Auth.UseCases;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ez.Hress.Auth.UnitTest.UseCases;

public class SessionInteractorTests
{
    private const string SessionKey = "session-key-session-key-session-key-0123456789";
    private static SessionInteractor Create(string key = SessionKey, string issuer = "issuer", double days = 30) =>
        new(new SessionInfo(key, issuer, TimeSpan.FromDays(days)), Mock.Of<ILogger<SessionInteractor>>());

    [Fact]
    public void CreateSession_RoundTrips()
    {
        var interactor = Create();
        Assert.Equal(220, interactor.GetUserID(interactor.CreateSession(220)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void GetUserID_Invalid_ReturnsNull(string? session)
    {
        Assert.Null(Create().GetUserID(session));
    }

    [Fact]
    public void GetUserID_SignedWithOtherKey_ReturnsNull()
    {
        var other = Create(key: "another-session-key-another-session-key-0123");
        Assert.Null(Create().GetUserID(other.CreateSession(220)));
    }

    [Fact]
    public void GetUserID_OtherIssuer_ReturnsNull()
    {
        Assert.Null(Create().GetUserID(Create(issuer: "someone-else").CreateSession(220)));
    }

    [Fact]
    public void GetUserID_AccessTokenWithSameKey_IsNotASession()
    {
        // An access token must never be accepted as a session, even if keys were (wrongly) shared.
        var accessTokens = new AuthenticationInteractor(new AuthenticationInfo(SessionKey, "issuer", "audience", "salt"),
            Mock.Of<IAuthenticationDataAccess>(), Mock.Of<ILogger<AuthenticationInteractor>>());
        Assert.Null(Create().GetUserID(accessTokens.CreateAccessToken(220)));
    }

    [Fact]
    public void CreateSession_InvalidUser_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create().CreateSession(0));
    }

    [Fact]
    public void Constructor_ShortKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(key: "short"));
    }
}
