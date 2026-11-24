using Ez.Hress.Auth.DataAccess;
using Ez.Hress.Auth.Entities;
using Ez.Hress.Auth.UseCases;
using Ez.Hress.Auth.FunctionsApi;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new ConfigurationBuilder()
    .AddJsonFile("host.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var corsPolicy = CorsPolicy.FromSetting(config["Ez.Hress.Auth.AllowedOrigins"]);
var dbConnectionInfo = new DbConnectionInfo(config["Ez.Hress.Database.ConnectionString"] ?? string.Empty);
var authenticationInfo = new AuthenticationInfo(
    config["Ez.Hress.Auth.Key"] ?? string.Empty,
    config["Ez.Hress.Auth.Issuer"] ?? string.Empty,
    config["Ez.Hress.Auth.Audience"] ?? string.Empty,
    config["Ez.Hress.Auth.Salt"] ?? string.Empty);
var sessionInfo = new SessionInfo(
    config["Ez.Hress.Auth.SessionKey"] ?? string.Empty,
    config["Ez.Hress.Auth.Issuer"] ?? string.Empty,
    TimeSpan.FromDays(int.TryParse(config["Ez.Hress.Auth.SessionDays"], out var sessionDays) ? sessionDays : 30));

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorsMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton(corsPolicy);
        services.AddSingleton(dbConnectionInfo);

        // Authentication
        services.AddSingleton(authenticationInfo);
        services.AddSingleton(sessionInfo);
        services.AddSingleton<IAuthenticationDataAccess, AuthenticationSqlAccess>();
        services.AddSingleton<AuthenticationInteractor>();
        services.AddSingleton<SessionInteractor>();
    })
    .Build();

host.Run();
