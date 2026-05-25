# Configuration Guide

## Location
`%APPDATA%\NotificationAggregator\config.json`

On Windows, this expands to:
```
C:\Users\YourUsername\AppData\Roaming\NotificationAggregator\config.json
```

## Default Configuration

```json
{
  "windows-eventlog": {
    "enabled": "true",
    "logs": "Application;System;Security"
  },
  "custom-api": {
    "enabled": "false",
    "webhookUrl": "",
    "apiKey": ""
  }
}
```

## Collectors

### Windows Event Log

**Config Section**: `windows-eventlog`

**Options**:
- `enabled`: "true" or "false"
- `logs`: Semicolon-separated list of event logs
  - Built-in: "Application", "System", "Security"
  - Custom: "MyApp", "Custom Logs", etc.

**Example**:
```json
{
  "windows-eventlog": {
    "enabled": "true",
    "logs": "Application;System;Security;Microsoft-Windows-PowerShell/Operational"
  }
}
```

**What it collects**:
- All events with severity Warning or higher
- Last 5 minutes (configurable in code)

### Custom API

**Config Section**: `custom-api`

**Options**:
- `enabled`: "true" or "false"
- `webhookUrl`: Your API endpoint (not yet implemented)
- `apiKey`: Authentication key (not yet implemented)

**Status**: Currently a placeholder. Implementation coming in Phase 2.

## Filters

Filters are stored in the SQLite database and managed programmatically (GUI dashboard coming in Phase 2).

To add a filter manually:

```powershell
# PowerShell snippet (requires SQLite client)
$db = "C:\Users\YourUsername\AppData\Roaming\NotificationAggregator\notifications.db"

# Suppress all "Info" severity Windows events
sqlite3 $db @"
INSERT INTO NotificationFilters 
(Name, SourcePattern, TitlePattern, MessagePattern, MinSeverity, Action, ActionValue, IsEnabled, CreatedAt)
VALUES 
('Suppress Info Events', 'windows-eventlog', '*', '*', 0, 0, '', 1, datetime('now'));
"@
```

**Filter Actions**:
- `0` = Suppress (hide from feed)
- `1` = Highlight (mark as critical in dashboard)
- `2` = Tag (add label)

**Patterns**: Wildcard matching
- `*` = Match anything
- `?` = Match single character
- `Log*` = Starts with "Log"
- `*Error*` = Contains "Error"

## Performance Tuning

### Collection Interval
Modify `AggregatorBackgroundService.cs`:
```csharp
// Default: 30 seconds
_collectionTimer = new Timer(..., TimeSpan.FromSeconds(30));
```

Lower values = more CPU, less delay

### Retention Period
Modify `AggregatorBackgroundService.cs`:
```csharp
// Default: 30 days
var cutoffDate = DateTime.Now.AddDays(-30);
```

## Troubleshooting

### Service won't start
1. Check logs: `%APPDATA%\NotificationAggregator\logs-*.txt`
2. Verify database exists: `%APPDATA%\NotificationAggregator\notifications.db`
3. Check config.json syntax (must be valid JSON)

### Missing notifications
1. Check if collector is enabled in config.json
2. Check if filter suppressed them: `IsFiltered = 1` in database
3. Review logs for collector errors

### High CPU usage
1. Increase collection interval
2. Add more filters to reduce data volume
3. Reduce number of monitored event logs

## Advanced: Custom Collectors

To add a Discord collector, for example:

1. Create `src/NotificationAggregator.Collectors/DiscordCollector.cs`
2. Implement `INotificationCollector`
3. Register in `AggregatorBackgroundService.InitializeCollectorsAsync()`
4. Add config section to `config.json`

See [ARCHITECTURE.md](../docs/ARCHITECTURE.md) for details.
