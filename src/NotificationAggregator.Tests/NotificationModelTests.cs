using NotificationAggregator.Core.Models;
using Xunit;

namespace NotificationAggregator.Tests;

public class NotificationModelTests
{
    [Fact]
    public void Notification_CanBeCreatedWithValidData()
    {
        // Arrange & Act
        var notification = new Notification
        {
            SourceId = "evt-123",
            Source = "Windows Event Log",
            Severity = SeverityLevel.Warning,
            Title = "System Warning",
            Message = "Disk space low",
            OccurredAt = DateTime.Now
        };
        
        // Assert
        Assert.NotNull(notification);
        Assert.Equal("evt-123", notification.SourceId);
        Assert.Equal(SeverityLevel.Warning, notification.Severity);
    }
    
    [Fact]
    public void Filter_CanBeCreatedWithWildcardPatterns()
    {
        // Arrange & Act
        var filter = new NotificationFilter
        {
            Name = "Suppress Info Logs",
            SourcePattern = "*EventLog*",
            TitlePattern = "*info*",
            MessagePattern = "*",
            MinSeverity = SeverityLevel.Info,
            Action = FilterAction.Suppress,
            IsEnabled = true
        };
        
        // Assert
        Assert.NotNull(filter);
        Assert.Equal(FilterAction.Suppress, filter.Action);
        Assert.True(filter.IsEnabled);
    }
}
