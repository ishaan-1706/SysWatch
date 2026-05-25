namespace NotificationAggregator.Core.Services;

using Interfaces;
using Models;

/// <summary>
/// Simple filter engine using pattern matching (no regex for now)
/// </summary>
public class SimpleFilterEngine : IFilterEngine
{
    private readonly IConfigurationService _configService;
    private List<NotificationFilter> _filters = new();
    
    public SimpleFilterEngine(IConfigurationService configService)
    {
        _configService = configService;
    }
    
    public async Task RefreshFiltersAsync()
    {
        _filters = await _configService.GetFiltersAsync();
    }
    
    public async Task<Notification> ApplyFiltersAsync(Notification notification)
    {
        await Task.CompletedTask; // Ensure method is truly async

        foreach (var filter in _filters.Where(f => f.IsEnabled))
        {
            if (MatchesFilter(notification, filter))
            {
                switch (filter.Action)
                {
                    case FilterAction.Suppress:
                        notification.IsFiltered = true;
                        break;
                    case FilterAction.Tag:
                        notification.Tags += $",{filter.ActionValue}";
                        break;
                    case FilterAction.Highlight:
                        notification.Severity = SeverityLevel.Critical;
                        break;
                }
            }
        }
        
        return notification;
    }
    
    private bool MatchesFilter(Notification notification, NotificationFilter filter)
    {
        // Check source
        if (filter.SourcePattern != "*" && !WildcardMatch(notification.Source, filter.SourcePattern))
            return false;
        
        // Check title
        if (filter.TitlePattern != "*" && !WildcardMatch(notification.Title, filter.TitlePattern))
            return false;
        
        // Check message
        if (filter.MessagePattern != "*" && !WildcardMatch(notification.Message, filter.MessagePattern))
            return false;
        
        // Check severity
        if (notification.Severity < filter.MinSeverity)
            return false;
        
        return true;
    }
    
    private bool WildcardMatch(string text, string pattern)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            text,
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }
}
