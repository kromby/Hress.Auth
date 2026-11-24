using Ez.Hress.Auth.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Ez.Hress.Auth.FunctionsApi;

/// <summary>
/// Adds CORS headers for allowed origins and answers preflight (OPTIONS) requests.
/// Runs inside the worker because the Static Web Apps managed API has no CORS setting of its own.
/// Every HTTP trigger that browsers on other sites call must also accept "options".
/// </summary>
public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private const string _class = nameof(CorsMiddleware);
    internal const string AllowedHeaders = "Content-Type, X-Custom-Authorization";
    internal const string AllowedMethods = "GET, POST, PUT, DELETE, OPTIONS";

    private readonly CorsPolicy _policy;
    private readonly ILogger<CorsMiddleware> _log;

    public CorsMiddleware(CorsPolicy policy, ILogger<CorsMiddleware> log)
    {
        _policy = policy;
        _log = log;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            await next(context);
            return;
        }

        var request = httpContext.Request;
        var response = httpContext.Response;
        var origin = request.Headers.Origin.ToString();
        var isPreflight = HttpMethods.IsOptions(request.Method);

        if (_policy.IsAllowed(origin))
        {
            response.Headers.AccessControlAllowOrigin = origin;
            response.Headers.Append("Vary", "Origin");
        }
        else if (!string.IsNullOrEmpty(origin))
        {
            _log.LogInformation("[{Class}.{Method}] Origin '{Origin}' is not allowed.", _class, nameof(Invoke), origin);
        }

        if (isPreflight)
        {
            if (_policy.IsAllowed(origin))
            {
                response.Headers.AccessControlAllowMethods = AllowedMethods;
                response.Headers.AccessControlAllowHeaders = AllowedHeaders;
                response.Headers.AccessControlMaxAge = "600";
            }

            response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }
}
