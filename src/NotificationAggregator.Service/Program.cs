using NotificationAggregator.Service;
using Serilog;

// Set up paths before anything else
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "NotificationAggregator"
);
Directory.CreateDirectory(appDataPath);

var dbPath = Path.Combine(appDataPath, "notifications.db");
var logsPath = Path.Combine(appDataPath, "logs-.txt");

// Configure logging first
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(logsPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Notification Aggregator starting...");
    Log.Information("App Data: {AppDataPath}", appDataPath);
    Log.Information("Database: {DbPath}", dbPath);

    var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .ConfigureServices(services =>
        {
            services.AddSingleton(new AggregatorBackgroundService(dbPath));
            services.AddHostedService(sp => sp.GetRequiredService<AggregatorBackgroundService>());
        })
        .UseSerilog()
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
