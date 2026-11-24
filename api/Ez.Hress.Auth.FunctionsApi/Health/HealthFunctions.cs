using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ez.Hress.Auth.FunctionsApi.Health;

public class HealthFunctions
{
    private const string _class = nameof(HealthFunctions);
    private readonly ILogger<HealthFunctions> _log;

    public HealthFunctions(ILogger<HealthFunctions> log)
    {
        _log = log;
    }

    /// <summary>Liveness check. Also used to verify CORS from client apps (accepts OPTIONS).</summary>
    [Function("health")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "health")] HttpRequest req)
    {
        _log.LogInformation("[{Class}.{Method}] Health check.", _class, nameof(Run));
        var version = typeof(HealthFunctions).Assembly.GetName().Version?.ToString() ?? "unknown";
        return new OkObjectResult(new { status = "ok", version });
    }
}
