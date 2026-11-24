using Ez.Hress.Auth.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ez.Hress.Auth.UseCases;

public class AuthenticationInteractor
{
    private const string _class = nameof(AuthenticationInteractor);
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    private readonly AuthenticationInfo _authenticationInfo;
    private readonly IAuthenticationDataAccess _dataAccess;
    private readonly ILogger<AuthenticationInteractor> _log;

    public AuthenticationInteractor(AuthenticationInfo authenticationInfo, IAuthenticationDataAccess dataAccess, ILogger<AuthenticationInteractor> log)
    {
        ArgumentNullException.ThrowIfNull(authenticationInfo);
        authenticationInfo.Validate();

        _authenticationInfo = authenticationInfo;
        _dataAccess = dataAccess;
        _log = log;
    }

    /// <summary>Checks username and password. Returns the user id, or throws <see cref="UnauthorizedAccessException"/>.</summary>
    public async Task<int> VerifyPasswordAsync(string username, string password, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));

        var user = await _dataAccess.GetUserByUsername(username);
        var hashed = PasswordHasher.Hash(password, _authenticationInfo.Salt);

        if (user == null || user.Value.UserID <= 0 || !PasswordHasher.Matches(hashed, user.Value.PasswordHash))
        {
            _log.LogWarning("[{Class}.{Method}] User '{Username}' not authenticated.", _class, nameof(VerifyPasswordAsync), username);
            throw new UnauthorizedAccessException();
        }

        await _dataAccess.SaveLoginInformation(user.Value.UserID, ipAddress);
        return user.Value.UserID;
    }

    /// <summary>Legacy: exchanges a one-use magic code (old site → SPA hand-over) for an access token.</summary>
    public async Task<string> LoginWithMagicCodeAsync(string code, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        var userID = await _dataAccess.RedeemMagicCode(code);
        if (userID <= 0)
        {
            _log.LogWarning("[{Class}.{Method}] Magic code is not valid.", _class, nameof(LoginWithMagicCodeAsync));
            throw new UnauthorizedAccessException();
        }

        await _dataAccess.SaveLoginInformation(userID, ipAddress);
        return CreateAccessToken(userID);
    }

    /// <summary>Access token in the same format Hress.Org issued: HS256, sub = user id, 7 days.</summary>
    public string CreateAccessToken(int userID)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userID.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationInfo.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            _authenticationInfo.Issuer,
            _authenticationInfo.Audience,
            claims,
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
