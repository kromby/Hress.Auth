using Ez.Hress.Auth.Entities;
using Ez.Hress.Auth.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ez.Hress.Auth.FunctionsApi.Login;

public class LoginFunctions
{
    private const string _class = nameof(LoginFunctions);
    private readonly AuthenticationInteractor _authenticationInteractor;
    private readonly SessionInteractor _sessionInteractor;
    private readonly ILogger<LoginFunctions> _log;

    public LoginFunctions(AuthenticationInteractor authenticationInteractor, SessionInteractor sessionInteractor, ILogger<LoginFunctions> log)
    {
        _authenticationInteractor = authenticationInteractor;
        _sessionInteractor = sessionInteractor;
        _log = log;
    }

    /// <summary>
    /// Password login for the auth app's own login page. Starts the auth-app session (cookie) and returns no token.
    /// Client apps get tokens through the OAuth flow (#791), never from this endpoint.
    /// Same-origin only: no "options" trigger, so browsers on other sites can't send it JSON.
    /// </summary>
    [Function("login")]
    public async Task<IActionResult> RunLogin([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequest req)
    {
        const string method = nameof(RunLogin);

        if (!RequestUtil.IsSameOriginOrNoOrigin(req))
        {
            _log.LogWarning("[{Class}.{Method}] Cross-site login attempt from '{Origin}' refused.", _class, method, req.Headers.Origin.ToString());
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        if (!(req.ContentType ?? string.Empty).StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            return new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);

        LoginBody? body;
        try
        {
            body = await req.ReadFromJsonAsync<LoginBody>();
        }
        catch (JsonException)
        {
            return new BadRequestResult();
        }

        try
        {
            var userID = await _authenticationInteractor.VerifyPasswordAsync(body?.Username ?? string.Empty, body?.Password ?? string.Empty, RequestUtil.GetClientIp(req));
            var session = _sessionInteractor.CreateSession(userID);

            req.HttpContext.Response.Cookies.Append(SessionInfo.CookieName, session, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = _sessionInteractor.Lifetime,
                IsEssential = true,
            });

            return new NoContentResult();
        }
        catch (ArgumentException)
        {
            return new BadRequestObjectResult("Username and password are required.");
        }
        catch (UnauthorizedAccessException)
        {
            return new UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[{Class}.{Method}] Unhandled error", _class, method);
            throw;
        }
    }

    /// <summary>
    /// Legacy: exchanges an old-site magic code for an access token (old site → Hress.Org SPA hand-over via /login/magic).
    /// Only accepts a code. Username/password login is not possible here. Codes are one-use.
    /// Called cross-site by the Hress.Org SPA, so it accepts "options" for the CORS preflight.
    /// </summary>
    [Function("authenticate")]
    public async Task<IActionResult> RunAuthenticate([HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "authenticate")] HttpRequest req)
    {
        const string method = nameof(RunAuthenticate);

        MagicCodeBody? body;
        try
        {
            body = await req.ReadFromJsonAsync<MagicCodeBody>();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return new BadRequestResult();
        }

        if (string.IsNullOrWhiteSpace(body?.Code))
        {
            _log.LogWarning("[{Class}.{Method}] Request with no code.", _class, method);
            return new UnauthorizedResult();
        }

        try
        {
            var jwt = await _authenticationInteractor.LoginWithMagicCodeAsync(body.Code, RequestUtil.GetClientIp(req));
            return new OkObjectResult(jwt);
        }
        catch (UnauthorizedAccessException)
        {
            return new UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[{Class}.{Method}] Unhandled error", _class, method);
            throw;
        }
    }
}
