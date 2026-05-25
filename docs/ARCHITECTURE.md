# Architecture

## Overview

The Notification Aggregator is a .NET 8 Windows Service that provides a unified interface for system and application notifications. It follows clean architecture principles with clear separation of concerns.

## Layers

### Core Layer (`NotificationAggregator.Core`)
Contains all business logic, models, and interfaces.

**Responsibilities**:
- Domain models (`Notification`, `NotificationFilter`)
- Service interfaces (`INotificationCollector`, `INotificationRepository`, `IConfigurationService`, `IFilterEngine`)
- Data access (SQLite repository)
- Filter processing logic

**Key Classes**:
- `Notification`: Represents a single notification event
- `NotificationFilter`: User-defined filter rules
- `INotificationCollector`: Interface for pluggable collectors
- `INotificationRepository`: Abstraction for data persistence
- `SimpleFilterEngine`: Pattern-matching filter processor

### Collector Layer (`NotificationAggregator.Collectors`)
Implements notification sources.

**Collectors**:
- `WindowsEventLogCollector`: Fetches from Windows Event Logs
- `CustomApiCollector`: Template for custom API/webhook sources
- Extensible for Discord, Teams, file monitoring, etc.

**Design**:
- All collectors implement `INotificationCollector`
- Pluggable architecture allows adding new sources without modifying core
- Each collector manages its own state (last check time, connection pool, etc.)

### Service Layer (`NotificationAggregator.Service`)
Windows Service entry point and orchestration.

**Responsibilities**:
- Initialize collectors, database, and filters
- Run background collection every 30 seconds
- Run daily cleanup of old notifications
- Graceful shutdown and error handling
- Structured logging via Serilog

## Data Flow

```
┌──────────────┐
│  Collectors  │
└──────┬───────┘
       │ (Raw notifications every 30s)
       ▼
┌──────────────────┐
│  Filter Engine   │ (Apply user rules)
└──────┬───────────┘
       │
       ▼
┌────────────────┐
│  Repository    │ (Persist to SQLite)
└────────┬───────┘
         │
         ▼
    ┌────────┐
    │ SQLite │
    └────────┘
```

## Database Schema

### Notifications
- `Id`: Primary key
- `SourceId`: Unique ID from source system
- `Source`: Collector name (e.g., "windows-eventlog")
- `Severity`: 0=Info, 1=Warning, 2=Error, 3=Critical
- `Title`, `Message`: Main content
- `Tags`: Comma-separated labels
- `OccurredAt`: Event timestamp
- `IngestedAt`: When we collected it
- `IsAcknowledged`: User read flag
- `IsFiltered`: Suppressed by filter
- `Metadata`: JSON extra data

### NotificationFilters
- `Id`: Primary key
- `Name`: Human-readable filter name
- `SourcePattern`: Wildcard match on source
- `TitlePattern`: Wildcard match on title
- `MessagePattern`: Wildcard match on message
- `MinSeverity`: Only match at/above this level
- `Action`: 0=Suppress, 1=Highlight, 2=Tag
- `ActionValue`: What to do (tag label, etc.)
- `IsEnabled`: On/off toggle
- `CreatedAt`: When filter was created

## Configuration

### config.json
Located at `%APPDATA%\NotificationAggregator\config.json`

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

Each collector reads its own config section.

## Extensibility

### Adding a New Collector

1. Create class in `Collectors/` inheriting `INotificationCollector`
2. Implement required methods:
   - `InitializeAsync()`: One-time setup
   - `IsHealthyAsync()`: Connection check
   - `CollectAsync()`: Fetch notifications
   - `ShutdownAsync()`: Cleanup
3. Register in `AggregatorBackgroundService.InitializeCollectorsAsync()`
4. Add config section to `config.json`

### Example: Discord Collector
```csharp
public class DiscordCollector : INotificationCollector
{
    public string Name => "discord";
    
    public async Task<List<Notification>> CollectAsync(...)
    {
        // Call Discord API for recent messages
        // Convert to Notification objects
        // Return list
    }
}
```

## Error Handling

- **Collector failure**: Logged, continues with other collectors
- **Database failure**: Logged, collection continues (in-memory buffer)
- **Filter failure**: Notification passes unfiltered
- **Service crash**: Windows Service auto-restart policy

## Performance

- **Collection**: Non-blocking, ~50ms per collector
- **Filtering**: In-memory, <1ms per notification
- **Database**: Indexed queries, <10ms for typical queries
- **Memory**: ~50 MB baseline (scales with retention)

## Deployment

### Windows Service
- Installed to `C:\Program Files\NotificationAggregator\` (or custom)
- Runs under Local System or specified account
- Auto-starts on boot
- Auto-restart on crash (configurable)

### Data Storage
- SQLite DB: `%APPDATA%\NotificationAggregator\notifications.db`
- Config: `%APPDATA%\NotificationAggregator\config.json`
- Logs: `%APPDATA%\NotificationAggregator\logs-*.txt` (daily rotation)

## Security

- **No authentication** (runs on single machine)
- **Config permissions**: File system ACLs
- **Database**: Local SQLite (no network exposure by default)
- **Logging**: Sanitized (no passwords/keys in logs)
- **Future**: API will have JWT + HTTPS

## Testing

- **Unit tests**: Filter logic, models
- **Integration tests**: Collector + database
- **xUnit framework**: Fast, parallel test execution

Run tests:
```powershell
dotnet test -c Release
```

## Monitoring

Log files at `%APPDATA%\NotificationAggregator\logs-YYYY-MM-DD.txt`

**Key log messages**:
- Service start/stop
- Collector initialization
- Notification counts per cycle
- Filter applications
- Cleanup operations
- Errors with full stack traces
