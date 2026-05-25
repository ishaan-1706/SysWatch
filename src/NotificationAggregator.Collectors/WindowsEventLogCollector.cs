namespace NotificationAggregator.Collectors;

using Core.Interfaces;
using Core.Models;
using System.Diagnostics.Eventing.Reader;
using Serilog;

/// <summary>
/// Collects notifications from Windows Event Logs
/// </summary>
public class WindowsEventLogCollector : INotificationCollector
{
    public string Name => "windows-eventlog";
    public string Description => "Collects notifications from Windows Event Logs";
    
    private Dictionary<string, string> _config = new();
    private DateTime _lastCheck = DateTime.MinValue;
    private readonly ILogger _logger;
    
    public WindowsEventLogCollector(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(Dictionary<string, string> config, CancellationToken cancellationToken)
    {
        _config = config;
        _lastCheck = DateTime.Now.AddMinutes(-5); // Start from 5 minutes ago
        _logger.Information("WindowsEventLogCollector initialized");
        await Task.CompletedTask;
    }
    
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Simple health check: ensure we can access event logs
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1).ToString("O");
            var query = new EventLogQuery("Application", PathType.LogName, $"*[System[TimeCreated[@SystemTime >= '{oneMinuteAgo}']]]");
            using (var reader = new EventLogReader(query))
            {
                // Just trying to create the reader is enough
                _ = reader;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "WindowsEventLogCollector health check failed");
            return false;
        }
    }
    
    public async Task<List<Notification>> CollectAsync(CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();
        
        try
        {
            var logNames = _config.ContainsKey("logs")
                ? _config["logs"].Split(';')
                : new[] { "Application", "System" };
            
            foreach (var logName in logNames)
            {
                try
                {
                    var query = $"*[System[TimeCreated[@SystemTime >= '{_lastCheck:O}']]]";
                    var eventLogQuery = new EventLogQuery(logName, PathType.LogName, query);
                    
                    using (var reader = new EventLogReader(eventLogQuery))
                    {
                        EventRecord? eventRecord;
                        while ((eventRecord = reader.ReadEvent()) != null)
                        {
                            if (eventRecord.Level >= (byte)StandardEventLevel.Warning)
                            {
                                notifications.Add(new Notification
                                {
                                    SourceId = eventRecord.RecordId?.ToString() ?? Guid.NewGuid().ToString(),
                                    Source = Name,
                                    Severity = MapEventLevel((StandardEventLevel?)eventRecord.Level ?? StandardEventLevel.Informational),
                                    Title = $"{logName}: {eventRecord.ProviderName}",
                                    Message = eventRecord.FormatDescription() ?? "No description available",
                                    OccurredAt = eventRecord.TimeCreated ?? DateTime.Now,
                                    Tags = $"{logName},{eventRecord.LevelDisplayName}",
                                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        EventId = eventRecord.Id,
                                        LogName = logName,
                                        ProviderName = eventRecord.ProviderName,
                                        ProcessId = eventRecord.ProcessId,
                                        ThreadId = eventRecord.ThreadId
                                    })
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error reading event log {LogName}", logName);
                }
            }
            
            _lastCheck = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in WindowsEventLogCollector.CollectAsync");
        }
        
        return notifications;
    }
    
    public Task ShutdownAsync()
    {
        _logger.Information("WindowsEventLogCollector shutting down");
        return Task.CompletedTask;
    }
    
    private SeverityLevel MapEventLevel(StandardEventLevel level) => level switch
    {
        StandardEventLevel.Critical => SeverityLevel.Critical,
        StandardEventLevel.Error => SeverityLevel.Error,
        StandardEventLevel.Warning => SeverityLevel.Warning,
        _ => SeverityLevel.Info
    };
}
