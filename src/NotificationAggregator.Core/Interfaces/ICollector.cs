namespace NotificationAggregator.Core.Interfaces;

using Models;

/// <summary>
/// Contract for all notification collectors (Windows Event Log, Discord, Teams, etc.)
/// </summary>
public interface INotificationCollector
{
    /// <summary>
    /// Unique identifier for this collector (e.g., "windows-eventlog", "discord")
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// User-friendly description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Initialize the collector with configuration
    /// </summary>
    Task InitializeAsync(Dictionary<string, string> config, CancellationToken cancellationToken);
    
    /// <summary>
    /// Check if collector is healthy and can connect to source
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Fetch notifications since last poll
    /// </summary>
    Task<List<Notification>> CollectAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Called when service shuts down
    /// </summary>
    Task ShutdownAsync();
}

/// <summary>
/// Repository pattern for notification persistence
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Save a new notification
    /// </summary>
    Task<int> AddAsync(Notification notification);
    
    /// <summary>
    /// Get notifications with filtering and pagination
    /// </summary>
    Task<(List<Notification> Items, int Total)> GetAsync(
        int pageNumber = 1,
        int pageSize = 50,
        SeverityLevel? minSeverity = null,
        string? source = null,
        bool? acknowledged = null,
        DateTime? since = null);
    
    /// <summary>
    /// Mark notification as acknowledged
    /// </summary>
    Task AcknowledgeAsync(int notificationId);
    
    /// <summary>
    /// Delete old notifications
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoffDate);
    
    /// <summary>
    /// Get count of unacknowledged notifications by severity
    /// </summary>
    Task<Dictionary<SeverityLevel, int>> GetUnacknowledgedCountBySeverityAsync();
}

/// <summary>
/// Configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get all collector configurations
    /// </summary>
    Dictionary<string, Dictionary<string, string>> GetCollectorConfigs();
    
    /// <summary>
    /// Get all filters
    /// </summary>
    Task<List<NotificationFilter>> GetFiltersAsync();
    
    /// <summary>
    /// Save/update a filter
    /// </summary>
    Task SaveFilterAsync(NotificationFilter filter);
    
    /// <summary>
    /// Delete a filter
    /// </summary>
    Task DeleteFilterAsync(int filterId);
}

/// <summary>
/// Filter engine to process notifications
/// </summary>
public interface IFilterEngine
{
    /// <summary>
    /// Load filters from repository
    /// </summary>
    Task RefreshFiltersAsync();
    
    /// <summary>
    /// Apply all filters to a notification
    /// </summary>
    Task<Notification> ApplyFiltersAsync(Notification notification);
}
