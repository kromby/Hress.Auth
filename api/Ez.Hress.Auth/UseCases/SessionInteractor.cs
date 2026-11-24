using Ez.Hress.Auth.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ez.Hress.Auth.UseCases;

/// <summary>
/// The auth app's login session: a signed JWT kept in an HttpOnly cookie on the auth domain.
/// It is never sent to client apps. They get access tokens through the OAuth flow.
/// </summary>
public class SessionInteractor
{
    private const string _class = nameof(SessionInteractor);
    private readonly SessionInfo _sessionInfo;
    private readonly ILogger<SessionInteractor> _log;
    private readonly SymmetricSecurityKey _key;

    public SessionInteractor(SessionInfo sessionInfo, ILogger<SessionInteractor> log)
    {
        ArgumentNullException.ThrowIfNull(sessionInfo);
        sessionInfo.Validate();

        _sessionInfo = sessionInfo;
        _log = log;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sessionInfo.Key));
    }

    public TimeSpan Lifetime => _sessionInfo.Lifetime;

    public string CreateSession(int userID)
    {
        if (userID <= 0)
            throw new ArgumentException("UserID must be greater than 0", nameof(userID));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            _sessionInfo.Issuer,
            SessionInfo.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userID.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.AuthTime, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            ],
            notBefore: now,
            expires: now.Add(_sessionInfo.Lifetime),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Returns the user id of a valid session, or null.</summary>
    public int? GetUserID(string? session)
    {
        if (string.IsNullOrWhiteSpace(session))
            return null;

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            var principal = handler.ValidateToken(session, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateIssuer = true,
                ValidIssuer = _sessionInfo.Issuer,
                ValidateAudience = true,
                ValidAudience = SessionInfo.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            }, out _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(sub, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var userID) && userID > 0 ? userID : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _log.LogInformation("[{Class}.{Method}] Session is not valid: {Reason}", _class, nameof(GetUserID), ex.GetType().Name);
            return null;
        }
    }
}
