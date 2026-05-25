using NotificationAggregator.Core.Models;
using NotificationAggregator.Core.Services;
using NotificationAggregator.Core.Interfaces;
using Moq;
using Xunit;

namespace NotificationAggregator.Tests;

public class FilterEngineTests
{
    [Fact]
    public async Task ApplyFilters_SuppressesMatchingNotifications()
    {
        // Arrange
        var mockConfigService = new Mock<IConfigurationService>();
        var filters = new List<NotificationFilter>
        {
            new()
            {
                Id = 1,
                Name = "Suppress Info",
                SourcePattern = "*",
                TitlePattern = "*",
                MessagePattern = "*",
                MinSeverity = SeverityLevel.Info,
                Action = FilterAction.Suppress,
                IsEnabled = true
            }
        };
        
        mockConfigService.Setup(x => x.GetFiltersAsync()).ReturnsAsync(filters);
        
        var engine = new SimpleFilterEngine(mockConfigService.Object);
        await engine.RefreshFiltersAsync();
        
        var notification = new Notification
        {
            Id = 1,
            SourceId = "test-1",
            Source = "TestSource",
            Severity = SeverityLevel.Info,
            Title = "Test",
            Message = "Test message",
            OccurredAt = DateTime.Now
        };
        
        // Act
        var result = await engine.ApplyFiltersAsync(notification);
        
        // Assert
        Assert.True(result.IsFiltered);
    }
    
    [Fact]
    public async Task ApplyFilters_HighlightsCriticalNotifications()
    {
        // Arrange
        var mockConfigService = new Mock<IConfigurationService>();
        var filters = new List<NotificationFilter>
        {
            new()
            {
                Id = 1,
                Name = "Highlight Errors",
                SourcePattern = "*",
                TitlePattern = "*",
                MessagePattern = "*",
                MinSeverity = SeverityLevel.Error,
                Action = FilterAction.Highlight,
                IsEnabled = true
            }
        };
        
        mockConfigService.Setup(x => x.GetFiltersAsync()).ReturnsAsync(filters);
        
        var engine = new SimpleFilterEngine(mockConfigService.Object);
        await engine.RefreshFiltersAsync();
        
        var notification = new Notification
        {
            Id = 1,
            SourceId = "test-1",
            Source = "TestSource",
            Severity = SeverityLevel.Error,
            Title = "Test Error",
            Message = "Error message",
            OccurredAt = DateTime.Now
        };
        
        // Act
        var result = await engine.ApplyFiltersAsync(notification);
        
        // Assert
        Assert.Equal(SeverityLevel.Critical, result.Severity);
    }
}
