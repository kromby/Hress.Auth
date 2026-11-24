using Ez.Hress.Auth.Entities;
using Ez.Hress.Auth.UseCases;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ez.Hress.Auth.UnitTest.UseCases;

public class AuthenticationInteractorTests
{
    private const string Key = "keyKEYkey1234.#keyKEYkey1234.#keyKEYkey1234.#keyKEYkey1234.#";
    private const string Salt = "salt";
    private const string Ip = "127.0.0.1";

    private readonly Mock<IAuthenticationDataAccess> _dataAccess = new();
    private readonly AuthenticationInteractor _interactor;

    public AuthenticationInteractorTests()
    {
        _interactor = new AuthenticationInteractor(new AuthenticationInfo(Key, "issuer", "audience", Salt), _dataAccess.Object, Mock.Of<ILogger<AuthenticationInteractor>>());
    }

    [Fact]
    public async Task VerifyPassword_CorrectPassword_ReturnsUserIdAndSavesLogin()
    {
        _dataAccess.Setup(x => x.GetUserByUsername("user")).ReturnsAsync((220, PasswordHasher.Hash("password", Salt)));

        var userID = await _interactor.VerifyPasswordAsync("user", "password", Ip);

        Assert.Equal(220, userID);
        _dataAccess.Verify(x => x.SaveLoginInformation(220, Ip), Times.Once);
    }

    [Fact]
    public async Task VerifyPassword_WrongPassword_Throws()
    {
        _dataAccess.Setup(x => x.GetUserByUsername("user")).ReturnsAsync((220, PasswordHasher.Hash("password", Salt)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _interactor.VerifyPasswordAsync("user", "wrong", Ip));
        _dataAccess.Verify(x => x.SaveLoginInformation(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyPassword_UnknownUser_Throws()
    {
        _dataAccess.Setup(x => x.GetUserByUsername("nobody")).ReturnsAsync(((int, string?)?)null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _interactor.VerifyPasswordAsync("nobody", "password", Ip));
    }

    [Fact]
    public async Task VerifyPassword_UserWithoutPassword_Throws()
    {
        _dataAccess.Setup(x => x.GetUserByUsername("user")).ReturnsAsync((220, (string?)null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _interactor.VerifyPasswordAsync("user", "password", Ip));
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("user", "")]
    [InlineData(" ", " ")]
    public async Task VerifyPassword_MissingInput_ThrowsArgumentNull(string username, string password)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _interactor.VerifyPasswordAsync(username, password, Ip));
    }

    [Fact]
    public async Task LoginWithMagicCode_ValidCode_ReturnsTokenForUser()
    {
        _dataAccess.Setup(x => x.RedeemMagicCode("code")).ReturnsAsync(220);

        var jwt = await _interactor.LoginWithMagicCodeAsync("code", Ip);

        Assert.Equal("220", ValidateAsHressOrgDoes(jwt));
        _dataAccess.Verify(x => x.SaveLoginInformation(220, Ip), Times.Once);
    }

    [Fact]
    public async Task LoginWithMagicCode_UsedOrUnknownCode_Throws()
    {
        _dataAccess.Setup(x => x.RedeemMagicCode("used")).ReturnsAsync(0);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _interactor.LoginWithMagicCodeAsync("used", Ip));
    }

    [Fact]
    public async Task LoginWithMagicCode_Missing_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _interactor.LoginWithMagicCodeAsync("", Ip));
    }

    [Fact]
    public void CreateAccessToken_ExpiresInSevenDays()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_interactor.CreateAccessToken(220));
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        Assert.Equal("220", token.Subject);
    }

    /// <summary>Validates exactly like Hress.Org's AuthenticationInteractor.DecryptToken, so its API keeps accepting our tokens.</summary>
    private static string ValidateAsHressOrgDoes(string token)
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidateIssuer = true,
            ValidIssuer = "issuer",
            ValidateAudience = true,
            ValidAudience = "audience",
        }, out _);
        return principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
    }
}
