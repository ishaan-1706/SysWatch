namespace NotificationAggregator.Service;

using Core.Data;
using Core.Interfaces;
using Core.Services;
using Collectors;
using Microsoft.Extensions.Hosting;
using Serilog;

/// <summary>
/// Main hosted service that runs the notification aggregator
/// </summary>
public class AggregatorBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly INotificationRepository _repository;
    private readonly IConfigurationService _configService;
    private readonly IFilterEngine _filterEngine;
    private List<INotificationCollector> _collectors = new();
    private Timer? _collectionTimer;
    private Timer? _cleanupTimer;
    
    private readonly string _dbPath;
    
    public AggregatorBackgroundService(string dbPath)
    {
        _dbPath = dbPath;
        
        // Initialize logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "logs.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        _logger = Log.ForContext<AggregatorBackgroundService>();
        
        // Initialize database and services
        var initializer = new DatabaseInitializer(_dbPath);
        initializer.Initialize();
        
        _repository = new SqliteNotificationRepository(_dbPath);
        var configPath = Path.Combine(Path.GetDirectoryName(_dbPath) ?? ".", "config.json");
        _configService = new ConfigurationService(configPath, _dbPath);
        _filterEngine = new SimpleFilterEngine(_configService);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Notification Aggregator Service starting...");
        
        try
        {
            // Initialize collectors
            await InitializeCollectorsAsync(stoppingToken);
            
            // Load filters
            await _filterEngine.RefreshFiltersAsync();
            
            // Start collection timer (every 30 seconds)
            _collectionTimer = new Timer(async _ => await CollectNotificationsAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            
            // Start cleanup timer (daily at 2 AM)
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(2);
            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);
            
            _cleanupTimer = new Timer(async _ => await CleanupOldNotificationsAsync(), null, nextRun - now, TimeSpan.FromDays(1));
            
            _logger.Information("Notification Aggregator Service started successfully");
            
            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Service failed to start");
            throw;
        }
    }
    
    private async Task InitializeCollectorsAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Initializing collectors...");
        
        var configs = _configService.GetCollectorConfigs();
        
        // Always add Windows Event Log collector if on Windows
        if (OperatingSystem.IsWindows())
        {
            var winCollector = new WindowsEventLogCollector(_logger);
            var winConfig = configs.ContainsKey("windows-eventlog") ? configs["windows-eventlog"] : new();
            await winCollector.InitializeAsync(winConfig, cancellationToken);
            
            if (await winCollector.IsHealthyAsync(cancellationToken))
            {
                _collectors.Add(winCollector);
                _logger.Information("Windows Event Log collector initialized");
            }
        }
        
        // Add custom API collector
        var customCollector = new CustomApiCollector(_logger);
        var customConfig = configs.ContainsKey("custom-api") ? configs["custom-api"] : new();
        await customCollector.InitializeAsync(customConfig, cancellationToken);
        _collectors.Add(customCollector);
        
        _logger.Information("Collectors initialized: {Count}", _collectors.Count);
    }
    
    private async Task CollectNotificationsAsync()
    {
        try
        {
            _logger.Debug("Starting notification collection cycle");
            
            foreach (var collector in _collectors)
            {
                try
                {
                    if (!await collector.IsHealthyAsync(CancellationToken.None))
                    {
                        _logger.Warning("Collector {Name} is not healthy", collector.Name);
                        continue;
                    }
                    
                    var notifications = await collector.CollectAsync(CancellationToken.None);
                    
                    foreach (var notification in notifications)
                    {
                        // Apply filters
                        var filteredNotification = await _filterEngine.ApplyFiltersAsync(notification);
                        
                        // Save to database
                        await _repository.AddAsync(filteredNotification);
                    }
                    
                    if (notifications.Count > 0)
                        _logger.Information("Collected {Count} notifications from {Collector}", notifications.Count, collector.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error collecting from {Collector}", collector.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in collection cycle");
        }
    }
    
    private async Task CleanupOldNotificationsAsync()
    {
        try
        {
            _logger.Information("Starting cleanup of old notifications");
            var cutoffDate = DateTime.Now.AddDays(-30); // Keep 30 days
            await _repository.DeleteOlderThanAsync(cutoffDate);
            _logger.Information("Cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during cleanup");
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Notification Aggregator Service stopping...");
        
        _collectionTimer?.Dispose();
        _cleanupTimer?.Dispose();
        
        foreach (var collector in _collectors)
        {
            try
            {
                await collector.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error shutting down {Collector}", collector.Name);
            }
        }
        
        await base.StopAsync(cancellationToken);
        _logger.Information("Notification Aggregator Service stopped");
    }
}
