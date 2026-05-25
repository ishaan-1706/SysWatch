namespace NotificationAggregator.Collectors;

using Core.Interfaces;
using Core.Models;
using Serilog;

/// <summary>
/// Dummy collector for testing and placeholder for custom app notifications
/// </summary>
public class CustomApiCollector : INotificationCollector
{
    public string Name => "custom-api";
    public string Description => "Collects from custom webhook/API endpoints";
    
    private Dictionary<string, string> _config = new();
    private readonly ILogger _logger;
    
    public CustomApiCollector(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(Dictionary<string, string> config, CancellationToken cancellationToken)
    {
        _config = config;
        _logger.Information("CustomApiCollector initialized");
        await Task.CompletedTask;
    }
    
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement actual health check when API endpoints are configured
        await Task.CompletedTask;
        return true;
    }
    
    public async Task<List<Notification>> CollectAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement actual collection from configured endpoints
        return new List<Notification>();
    }
    
    public Task ShutdownAsync()
    {
        _logger.Information("CustomApiCollector shutting down");
        return Task.CompletedTask;
    }
}
