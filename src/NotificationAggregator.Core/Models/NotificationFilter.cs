namespace NotificationAggregator.Core.Models;

/// <summary>
/// User-defined filter rules to suppress or categorize notifications
/// </summary>
public class NotificationFilter
{
    public int Id { get; set; }
    
    /// <summary>
    /// Human-readable filter name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Source to match (e.g., "Windows EventLog", "Discord", "*" for all)
    /// </summary>
    public string SourcePattern { get; set; } = "*";
    
    /// <summary>
    /// Title pattern (wildcard or regex)
    /// </summary>
    public string TitlePattern { get; set; } = "*";
    
    /// <summary>
    /// Message pattern (wildcard or regex)
    /// </summary>
    public string MessagePattern { get; set; } = "*";
    
    /// <summary>
    /// Minimum severity to match (anything >= this)
    /// </summary>
    public SeverityLevel MinSeverity { get; set; }
    
    /// <summary>
    /// Action: Suppress, Highlight, or Tag
    /// </summary>
    public FilterAction Action { get; set; }
    
    /// <summary>
    /// What to do (suppress=ignore, highlight=mark important, tag=add labels)
    /// </summary>
    public string ActionValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this filter enabled?
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// When was this filter created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

public enum FilterAction
{
    Suppress = 0,    // Hide from dashboard
    Highlight = 1,   // Mark as important
    Tag = 2          // Add labels
}
