namespace NotificationAggregator.Core.Models;

/// <summary>
/// Represents a single notification from any source
/// </summary>
public class Notification
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier from source system (Windows Event ID, App notification ID, etc.)
    /// </summary>
    public string SourceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Which system/app this came from (Windows EventLog, Discord, Teams, Custom API, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity: Info, Warning, Error, Critical
    /// </summary>
    public SeverityLevel Severity { get; set; }
    
    /// <summary>
    /// Short title of the notification
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Full message content
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Tags for categorization and filtering
    /// </summary>
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// When the notification occurred at source
    /// </summary>
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// When we ingested it
    /// </summary>
    public DateTime IngestedAt { get; set; }
    
    /// <summary>
    /// Has the user acknowledged this?
    /// </summary>
    public bool IsAcknowledged { get; set; }
    
    /// <summary>
    /// When it was marked as read
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Did the filter engine suppress this?
    /// </summary>
    public bool IsFiltered { get; set; }
}

public enum SeverityLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
