namespace NotificationAggregator.Collectors;

using Core.Interfaces;
using Core.Models;
using Serilog;
using System.Text.Json;

/// <summary>
/// Collects notification history from Windows.UI.Notifications
/// Captures toast notifications from Edge, Teams, Discord, etc.
/// </summary>
public class WindowsToastNotificationCollector : INotificationCollector
{
    public string Name => "windows-toast";
    public string Description => "Collects Windows toast notifications from Edge, Teams, etc.";
    
    private Dictionary<string, string> _config = new();
    private readonly ILogger _logger;
    private HashSet<string> _seenNotifications = new();
    
    public WindowsToastNotificationCollector(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(Dictionary<string, string> config, CancellationToken cancellationToken)
    {
        _config = config;
        _logger.Information("WindowsToastNotificationCollector initialized");
        await Task.CompletedTask;
    }
    
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        // Windows notifications are always available on Windows 10+
        await Task.CompletedTask;
        return OperatingSystem.IsWindowsVersionAtLeast(10, 0);
    }
    
    public async Task<List<Notification>> CollectAsync(CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();
        
        try
        {
            // Check Edge notification database
            var edgeDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft\\Edge\\User Data\\Default"
            );
            
            // This is a placeholder - Windows toast notifications are complex to capture
            // In a real implementation, you'd use Windows.UI.Notifications API
            // For now, this documents how it would work
            
            // TODO: Implement using Windows.UI.Notifications.Management
            // var manager = await ToastNotificationManager.GetDefault();
            // var history = manager.GetHistory();
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error collecting toast notifications");
        }
        
        return notifications;
    }
    
    public async Task ShutdownAsync()
    {
        _logger.Information("WindowsToastNotificationCollector shutting down");
        await Task.CompletedTask;
    }
}
