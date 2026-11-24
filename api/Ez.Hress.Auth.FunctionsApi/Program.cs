using Ez.Hress.Auth.Entities;
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
    })
    .Build();

host.Run();
